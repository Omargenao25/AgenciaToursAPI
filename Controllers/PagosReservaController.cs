using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Models;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace AgenciaToursAPI.Controllers;

[ApiController]
[Authorize(Roles = Roles.Personal)]
[Route("api/reservas/{reservaId:int}/pagos")]
public sealed class PagosReservaController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Estado(int reservaId, CancellationToken ct)
    {
        var reserva = await db.Reservas.AsNoTracking()
            .Where(r => r.Id == reservaId)
            .Select(r => new {
                r.Id, r.Total, r.Estado,
                r.VencePendienteUtc, r.MotivoCancelacion,
                FechaTour = r.Tour != null ? r.Tour.Fecha : (DateTime?)null,
                HoraTour = r.Tour != null ? r.Tour.Hora : (TimeSpan?)null,
                PorcentajeAdelanto = r.Tour != null ? r.Tour.PorcentajeAdelanto : 30,
                Cliente = r.Cliente != null ? r.Cliente.Nombre + " " + r.Cliente.Apellido : "",
                Tour = r.Tour != null ? r.Tour.Nombre : ""
            }).SingleOrDefaultAsync(ct);
        if (reserva is null) return NotFound("Reserva no encontrada.");

        var pagos = await db.PagosReserva.AsNoTracking()
            .Where(p => p.ReservaId == reservaId)
            .OrderBy(p => p.FechaRegistroUtc).ThenBy(p => p.Id)
            .Select(p => new PagoReservaRespuestaDto {
                Id = p.Id,
                Monto = p.Monto,
                MetodoPago = p.MetodoPago.Nombre,
                FechaRegistroUtc = p.FechaRegistroUtc,
                Referencia = p.Referencia,
                Observacion = p.Observacion,
                RegistradoPor = p.RegistradoPor
            }).ToListAsync(ct);
        var devuelto = await db.ReembolsosReserva.AsNoTracking()
            .Where(x => x.ReservaId == reservaId)
            .SumAsync(x => (decimal?)x.Monto, ct) ?? 0m;
        return Ok(new EstadoPagosReservaDto {
            ReservaId = reserva.Id, Cliente = reserva.Cliente, Tour = reserva.Tour,
            EstadoReserva = reserva.Estado, Total = reserva.Total,
            VencePendienteUtc = reserva.VencePendienteUtc,
            MotivoCancelacion = reserva.MotivoCancelacion,
            FechaTour = reserva.FechaTour, HoraTour = reserva.HoraTour,
            PorcentajeAdelanto = reserva.PorcentajeAdelanto,
            Pagado = pagos.Sum(p => p.Monto), Devuelto = devuelto, Pagos = pagos
        });
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(int reservaId, RegistrarPagoReservaDto dto, CancellationToken ct)
    {
        if (reservaId <= 0) return NotFound();
        if (dto.ClaveOperacion == Guid.Empty)
            return BadRequest("Se requiere una clave de operación válida.");
        if (dto.Monto <= 0 || decimal.Round(dto.Monto, 2) != dto.Monto)
            return BadRequest("El importe debe ser positivo y tener como máximo dos decimales.");
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var operadorId))
            return Unauthorized();

        // La reserva es el punto único de serialización para pagos, cancelación y edición.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var reserva = await db.Reservas
            .FromSqlInterpolated($"SELECT * FROM dbo.Reservas WITH (UPDLOCK, HOLDLOCK) WHERE Id = {reservaId}")
            .SingleOrDefaultAsync(ct);
        if (reserva is null) return NotFound("Reserva no encontrada.");

        var pagoPrevio = await db.PagosReserva.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ClaveOperacion == dto.ClaveOperacion, ct);
        if (pagoPrevio is not null)
        {
            if (pagoPrevio.ReservaId != reservaId ||
                pagoPrevio.Monto != dto.Monto ||
                pagoPrevio.MetodoPagoId != dto.MetodoPagoId ||
                pagoPrevio.Referencia != (dto.Referencia ?? "").Trim() ||
                pagoPrevio.Observacion != (dto.Observacion ?? "").Trim())
                return Conflict("Esta clave de operación ya se utilizó con datos diferentes.");
            return Ok(new { pagoPrevio.Id, pagoPrevio.ReservaId, pagoPrevio.Monto, Repetida = true });
        }

        if (reserva.Estado == EstadosReserva.Cancelada)
            return Conflict("No se pueden registrar pagos en reservas canceladas.");
        // No registrar el primer pago después de que expiró el plazo.
        // Si ya hay pagos, la reserva queda protegida para conciliación manual.
        var tienePagosPrevios = await db.PagosReserva.AnyAsync(p => p.ReservaId == reservaId, ct);
        if (reserva.Estado == EstadosReserva.Pendiente &&
            reserva.VencePendienteUtc is DateTime vencimiento &&
            vencimiento <= DateTime.UtcNow && !tienePagosPrevios)
            return Conflict("El plazo para registrar el adelanto venció. Esta reserva se cancelará automáticamente.");
        if (!await db.MetodosPago.AnyAsync(m => m.Id == dto.MetodoPagoId, ct))
            return BadRequest("Método de pago no válido.");

        var pagado = await db.PagosReserva
            .Where(p => p.ReservaId == reservaId)
            .SumAsync(p => (decimal?)p.Monto, ct) ?? 0m;
        var saldo = reserva.Total - pagado;
        if (dto.Monto > saldo)
            return Conflict($"El monto supera el saldo pendiente de RD$ {Math.Max(0, saldo):N2}.");

        var operador = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == operadorId && (u.Rol == Roles.Administrador || u.Rol == Roles.Operador))
            .Select(u => u.Nombre).SingleOrDefaultAsync(ct);
        if (operador is null) return Forbid();

        var pago = new PagoReserva {
            ReservaId = reservaId, Monto = dto.Monto,
            MetodoPagoId = dto.MetodoPagoId,
            Referencia = (dto.Referencia ?? "").Trim(),
            Observacion = (dto.Observacion ?? "").Trim(),
            RegistradoPorUsuarioId = operadorId,
            RegistradoPor = operador,
            FechaRegistroUtc = DateTime.UtcNow,
            ClaveOperacion = dto.ClaveOperacion
        };
        db.PagosReserva.Add(pago);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return StatusCode(StatusCodes.Status201Created, new { pago.Id, pago.ReservaId, pago.Monto, Saldo = saldo - pago.Monto });
    }
}
