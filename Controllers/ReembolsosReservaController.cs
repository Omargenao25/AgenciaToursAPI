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
[Route("api/reservas/{reservaId:int}/reembolsos")]
public sealed class ReembolsosReservaController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Historial(int reservaId, CancellationToken ct)
    {
        if (!await db.Reservas.AnyAsync(r => r.Id == reservaId, ct))
            return NotFound("No existe la reserva.");
        var items = await db.ReembolsosReserva.AsNoTracking()
            .Where(r => r.ReservaId == reservaId)
            .OrderBy(r => r.FechaRegistroUtc).ThenBy(r => r.Id)
            .Select(r => new ReembolsoRespuestaDto {
                Id = r.Id, Monto = r.Monto, MetodoPago = r.MetodoPago.Nombre,
                FechaRegistroUtc = r.FechaRegistroUtc, Referencia = r.Referencia,
                Motivo = r.Motivo, Observacion = r.Observacion,
                RegistradoPor = r.RegistradoPor
            }).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(int reservaId, RegistrarReembolsoDto dto, CancellationToken ct)
    {
        if (reservaId <= 0) return NotFound();
        if (dto.ClaveOperacion == Guid.Empty)
            return BadRequest("La clave de operación es obligatoria.");
        if (dto.Monto <= 0 || decimal.Round(dto.Monto, 2) != dto.Monto)
            return BadRequest("El monto debe ser positivo y tener como máximo dos decimales.");

        var motivo = (dto.Motivo ?? "").Trim();
        var referencia = (dto.Referencia ?? "").Trim();
        var observacion = (dto.Observacion ?? "").Trim();
        if (motivo.Length is < 8 or > 200)
            return BadRequest("Especifica un motivo entre 8 y 200 caracteres.");
        if (referencia.Length > 100 || observacion.Length > 200)
            return BadRequest("Referencia u observación demasiado larga.");

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var operadorId))
            return Unauthorized();
        var operador = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == operadorId && (u.Rol == Roles.Administrador || u.Rol == Roles.Operador))
            .Select(u => u.Nombre).SingleOrDefaultAsync(ct);
        if (operador is null) return Forbid();

        // Bloqueo común con abonos, confirmación, cancelación y vencimiento.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var reserva = await db.Reservas
            .FromSqlInterpolated($"SELECT * FROM dbo.Reservas WITH (UPDLOCK, HOLDLOCK) WHERE Id = {reservaId}")
            .SingleOrDefaultAsync(ct);
        if (reserva is null) return NotFound("No existe la reserva.");

        var previo = await db.ReembolsosReserva.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClaveOperacion == dto.ClaveOperacion, ct);
        if (previo is not null)
        {
            if (previo.ReservaId != reservaId || previo.Monto != dto.Monto ||
                previo.MetodoPagoId != dto.MetodoPagoId || previo.Motivo != motivo ||
                previo.Referencia != referencia || previo.Observacion != observacion)
                return Conflict("La clave de operación ya se utilizó con otros datos.");
            return Ok(new { previo.Id, previo.ReservaId, previo.Monto, Repetida = true });
        }

        if (reserva.Estado == EstadosReserva.Cancelada)
            return Conflict("La reserva está cancelada; no se admiten nuevos reembolsos en este flujo.");
        if (!await db.MetodosPago.AnyAsync(m => m.Id == dto.MetodoPagoId, ct))
            return BadRequest("El método de devolución no existe.");

        var cobrado = await db.PagosReserva.Where(x => x.ReservaId == reservaId)
            .SumAsync(x => (decimal?)x.Monto, ct) ?? 0m;
        var devuelto = await db.ReembolsosReserva.Where(x => x.ReservaId == reservaId)
            .SumAsync(x => (decimal?)x.Monto, ct) ?? 0m;
        var disponible = cobrado - devuelto;
        if (dto.Monto > disponible)
            return Conflict($"El importe supera lo pendiente de devolver (RD$ {Math.Max(0m, disponible):N2}).");

        db.ReembolsosReserva.Add(new ReembolsoReserva {
            ReservaId = reservaId, MetodoPagoId = dto.MetodoPagoId,
            Monto = dto.Monto, Motivo = motivo, Referencia = referencia,
            Observacion = observacion, FechaRegistroUtc = DateTime.UtcNow,
            RegistradoPorUsuarioId = operadorId, RegistradoPor = operador,
            ClaveOperacion = dto.ClaveOperacion
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return StatusCode(201, new { ReservaId = reservaId, Monto = dto.Monto,
            PendienteDeDevolver = disponible - dto.Monto });
    }
}
