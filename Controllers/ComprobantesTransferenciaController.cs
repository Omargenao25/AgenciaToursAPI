using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers;

// Una declaración de transferencia NO es un pago. Sólo el personal registra el pago real.
[ApiController]
[Authorize]
[Route("api/comprobantes-transferencia")]
public sealed class ComprobantesTransferenciaController(ApplicationDbContext db) : ControllerBase
{
    private const int MaxBytes = 2 * 1024 * 1024;

    public sealed class ReportarDto
    {
        [Range(typeof(decimal), "0.01", "10000000")]
        public decimal Monto { get; set; }
        [Required, StringLength(100, MinimumLength = 4)]
        public string Referencia { get; set; } = "";
        [Required]
        public string ArchivoBase64 { get; set; } = "";
    }

    public sealed class RechazarDto
    {
        [Required, StringLength(300, MinimumLength = 5)]
        public string Motivo { get; set; } = "";
    }

    private async Task<int?> ClienteIdAsync(CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)) return null;
        return await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId && u.Rol == Roles.Cliente)
            .Select(u => u.ClienteId).SingleOrDefaultAsync(ct);
    }

    private static string? TipoArchivo(byte[] bytes)
    {
        if (bytes.Length < 12 || bytes.Length > MaxBytes) return null;
        if (bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff &&
            bytes[^2] == 0xff && bytes[^1] == 0xd9) return "image/jpeg";
        if (bytes.Length >= 24 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) &&
            bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8)) return "image/png";
        if (bytes.AsSpan(0,4).SequenceEqual("RIFF"u8) &&
            bytes.AsSpan(8,4).SequenceEqual("WEBP"u8)) return "image/webp";
        if (bytes.AsSpan(0,5).SequenceEqual("%PDF-"u8) &&
            System.Text.Encoding.ASCII.GetString(bytes, Math.Max(0, bytes.Length-1024),
                Math.Min(1024, bytes.Length)).Contains("%%EOF", StringComparison.Ordinal))
            return "application/pdf";
        return null;
    }

    private static object Resumen(ComprobanteTransferencia c) => new
    {
        c.Id, c.ReservaId, c.ClienteId, c.MontoReportado, c.Referencia, c.Estado,
        c.CreadoUtc, c.RevisadoUtc, c.MotivoRevision, c.PagoReservaId
    };

    [Authorize(Roles = Roles.Cliente)]
    [HttpGet("mis-reservas/{reservaId:int}")]
    public async Task<IActionResult> MisComprobantes(int reservaId, CancellationToken ct)
    {
        var clienteId = await ClienteIdAsync(ct);
        if (clienteId is null) return Unauthorized();
        if (!await db.Reservas.AnyAsync(r => r.Id == reservaId && r.ClienteId == clienteId, ct))
            return NotFound();
        var items = await db.ComprobantesTransferencia.AsNoTracking()
            .Where(x => x.ReservaId == reservaId && x.ClienteId == clienteId)
            .OrderByDescending(x => x.CreadoUtc)
            .Select(x => new { x.Id, x.ReservaId, x.MontoReportado, x.Referencia,
                x.Estado, x.CreadoUtc, x.RevisadoUtc, x.MotivoRevision, x.PagoReservaId })
            .ToListAsync(ct);
        return Ok(items);
    }

    [Authorize(Roles = Roles.Cliente)]
    [HttpPost("mis-reservas/{reservaId:int}")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Reportar(int reservaId, ReportarDto dto, CancellationToken ct)
    {
        var clienteId = await ClienteIdAsync(ct);
        if (clienteId is null) return Unauthorized();
        if (dto.Monto <= 0 || decimal.Round(dto.Monto, 2) != dto.Monto)
            return BadRequest("Indica un monto válido con dos decimales.");
        if (dto.ArchivoBase64.Length > 4 * MaxBytes / 3 + 8)
            return BadRequest("El comprobante supera 2 MB.");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(dto.ArchivoBase64); }
        catch (FormatException) { return BadRequest("El archivo enviado no es válido."); }
        var mime = TipoArchivo(bytes);
        if (mime is null) return BadRequest("Solo PDF, JPG, PNG o WebP reales de hasta 2 MB.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var reserva = await db.Reservas
            .FromSqlInterpolated($"SELECT * FROM dbo.Reservas WITH (UPDLOCK, HOLDLOCK) WHERE Id = {reservaId} AND ClienteId = {clienteId.Value}")
            .SingleOrDefaultAsync(ct);
        if (reserva is null) return NotFound();
        if (reserva.Estado != EstadosReserva.Pendiente)
            return Conflict("Solo se pueden reportar adelantos de reservas pendientes.");
        if (reserva.VencePendienteUtc is DateTime vence && vence <= DateTime.UtcNow)
            return Conflict("El plazo de adelanto venció. Contacta a la agencia.");
        if (await db.ComprobantesTransferencia.AnyAsync(x => x.ReservaId == reservaId && x.Estado == "Pendiente", ct))
            return Conflict("Ya existe un comprobante esperando revisión para esta reserva.");
        var pagado = await db.PagosReserva.Where(x => x.ReservaId == reservaId)
            .SumAsync(x => (decimal?)x.Monto, ct) ?? 0m;
        var devuelto = await db.ReembolsosReserva.Where(x => x.ReservaId == reservaId)
            .SumAsync(x => (decimal?)x.Monto, ct) ?? 0m;
        if (dto.Monto > reserva.Total - (pagado - devuelto))
            return Conflict("El importe reportado supera el saldo pendiente.");

        var registro = new ComprobanteTransferencia
        {
            ReservaId = reservaId, ClienteId = clienteId.Value, MontoReportado = dto.Monto,
            Referencia = dto.Referencia.Trim(), ArchivoMime = mime,
            Archivo = bytes, CreadoUtc = DateTime.UtcNow
        };
        db.ComprobantesTransferencia.Add(registro);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return StatusCode(StatusCodes.Status201Created, Resumen(registro));
    }

    [Authorize(Roles = Roles.Personal)]
    [HttpGet("pendientes")]
    public async Task<IActionResult> Pendientes(CancellationToken ct)
    {
        var registros = await db.ComprobantesTransferencia.AsNoTracking()
            .Where(x => x.Estado == "Pendiente")
            .OrderBy(x => x.CreadoUtc)
            .Select(x => new {
                x.Id, x.ReservaId, x.MontoReportado, x.Referencia, x.CreadoUtc,
                Cliente = x.Cliente.Nombre + " " + x.Cliente.Apellido,
                Tour = x.Reserva.Tour != null ? x.Reserva.Tour.Nombre : ""
            }).Take(100).ToListAsync(ct);
        return Ok(registros);
    }

    [Authorize(Roles = Roles.Personal)]
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detalle(long id, CancellationToken ct)
    {
        var item = await db.ComprobantesTransferencia.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new {
                x.Id, x.ReservaId, x.MontoReportado, x.Referencia, x.Estado,
                x.CreadoUtc, x.RevisadoUtc, x.MotivoRevision, x.PagoReservaId,
                Cliente = x.Cliente.Nombre + " " + x.Cliente.Apellido,
                Tour = x.Reserva.Tour != null ? x.Reserva.Tour.Nombre : ""
            }).SingleOrDefaultAsync(ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:long}/archivo")]
    public async Task<IActionResult> Archivo(long id, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();
        var personal = User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.Operador);
        var clienteId = personal ? null : await ClienteIdAsync(ct);
        if (!personal && clienteId is null) return NotFound();
        var registro = await db.ComprobantesTransferencia.AsNoTracking()
            .Where(x => x.Id == id && (personal || x.ClienteId == clienteId))
            .Select(x => new { x.Archivo, x.ArchivoMime }).SingleOrDefaultAsync(ct);
        if (registro is null) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "sandbox";
        var extension = registro.ArchivoMime switch
        {
            "image/png" => ".png", "image/jpeg" => ".jpg",
            "image/webp" => ".webp", "application/pdf" => ".pdf", _ => ".bin"
        };
        return File(registro.Archivo, "application/octet-stream", "comprobante-" + id + extension);
    }

    [Authorize(Roles = Roles.Personal)]
    [HttpPost("{id:long}/aprobar")]
    public async Task<IActionResult> Aprobar(long id, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var operadorId))
            return Unauthorized();
        var operador = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == operadorId && (u.Rol == Roles.Administrador || u.Rol == Roles.Operador))
            .Select(u => u.Nombre).SingleOrDefaultAsync(ct);
        if (operador is null) return Forbid();

        var reservaId = await db.ComprobantesTransferencia.AsNoTracking()
            .Where(x => x.Id == id).Select(x => (int?)x.ReservaId).SingleOrDefaultAsync(ct);
        if (!reservaId.HasValue) return NotFound();

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var reserva = await db.Reservas
            .FromSqlInterpolated($"SELECT * FROM dbo.Reservas WITH (UPDLOCK, HOLDLOCK) WHERE Id = {reservaId.Value}")
            .SingleOrDefaultAsync(ct);
        if (reserva is null) return NotFound();
        var c = await db.ComprobantesTransferencia
            .FromSqlInterpolated($"SELECT * FROM dbo.ComprobantesTransferencia WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
            .SingleOrDefaultAsync(ct);
        if (c is null || c.ReservaId != reserva.Id) return NotFound();
        if (c.Estado == "Aprobado") return Ok(new { c.Id, c.PagoReservaId, Repetida = true });
        if (c.Estado != "Pendiente") return Conflict("Este comprobante ya fue rechazado.");
        if (reserva.Estado != EstadosReserva.Pendiente)
            return Conflict("La reserva ya no está pendiente. No se puede aprobar el pago.");
        // Si un comprobante fue reportado a tiempo, sólo se permite validar
        // dentro del margen finito de revisión tras el vencimiento original.
        if (reserva.VencePendienteUtc is DateTime vencimiento &&
            DateTime.UtcNow >= vencimiento.AddHours(2) &&
            !await db.PagosReserva.AnyAsync(p => p.ReservaId == reserva.Id, ct))
            return Conflict("Venció el margen de revisión de dos horas. No se puede aprobar este comprobante.");
        if (await db.CierresOperativosTour.AnyAsync(t => t.TourId == reserva.TourId, ct))
            return Conflict("La excursión ya tiene cierre operativo.");
        var pagado = await db.PagosReserva.Where(x => x.ReservaId == reserva.Id)
            .SumAsync(x => (decimal?)x.Monto, ct) ?? 0m;
        var devuelto = await db.ReembolsosReserva.Where(x => x.ReservaId == reserva.Id)
            .SumAsync(x => (decimal?)x.Monto, ct) ?? 0m;
        if (c.MontoReportado > reserva.Total - (pagado - devuelto))
            return Conflict("El importe supera el saldo. Revisa los pagos ya registrados.");

        var metodoId = await db.MetodosPago.AsNoTracking()
            .Where(m => EF.Functions.Like(m.Nombre, "%transfer%"))
            .OrderBy(m => m.Id).Select(m => (int?)m.Id).FirstOrDefaultAsync(ct);
        if (!metodoId.HasValue)
            return Conflict("Primero registra un método de pago llamado Transferencia bancaria.");

        var pago = new PagoReserva
        {
            ReservaId = reserva.Id, Monto = c.MontoReportado,
            MetodoPagoId = metodoId.Value, Referencia = c.Referencia,
            Observacion = "Transferencia verificada por personal · comprobante #" + c.Id,
            RegistradoPorUsuarioId = operadorId, RegistradoPor = operador,
            FechaRegistroUtc = DateTime.UtcNow, ClaveOperacion = Guid.NewGuid()
        };
        db.PagosReserva.Add(pago);
        await db.SaveChangesAsync(ct);
        c.Estado = "Aprobado";
        c.RevisadoUtc = DateTime.UtcNow;
        c.RevisadoPorUsuarioId = operadorId;
        c.PagoReservaId = pago.Id;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        // No se confirma automáticamente la reserva: personal utilizará la regla habitual.
        return Ok(new { c.Id, c.PagoReservaId, Monto = pago.Monto, Estado = c.Estado });
    }

    [Authorize(Roles = Roles.Personal)]
    [HttpPost("{id:long}/rechazar")]
    public async Task<IActionResult> Rechazar(long id, RechazarDto dto, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var operadorId))
            return Unauthorized();
        var reservaId = await db.ComprobantesTransferencia.AsNoTracking()
            .Where(x => x.Id == id).Select(x => (int?)x.ReservaId).SingleOrDefaultAsync(ct);
        if (!reservaId.HasValue) return NotFound();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Misma secuencia de bloqueos que aprobación y registro de pagos.
        var reserva = await db.Reservas
            .FromSqlInterpolated($"SELECT * FROM dbo.Reservas WITH (UPDLOCK, HOLDLOCK) WHERE Id = {reservaId.Value}")
            .SingleOrDefaultAsync(ct);
        if (reserva is null) return NotFound();
        var c = await db.ComprobantesTransferencia
            .FromSqlInterpolated($"SELECT * FROM dbo.ComprobantesTransferencia WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
            .SingleOrDefaultAsync(ct);
        if (c is null || c.ReservaId != reserva.Id) return NotFound();
        if (c.Estado != "Pendiente") return Conflict("El comprobante ya fue revisado.");
        c.Estado = "Rechazado";
        c.MotivoRevision = dto.Motivo.Trim();
        c.RevisadoUtc = DateTime.UtcNow;
        c.RevisadoPorUsuarioId = operadorId;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return NoContent();
    }
}
