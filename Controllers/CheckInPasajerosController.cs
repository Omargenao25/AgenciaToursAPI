using AgenciaToursAPI.Data;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Data;

namespace AgenciaToursAPI.Controllers;

[ApiController]
[Authorize(Roles = Roles.Personal)]
[Route("api/tours/{tourId:int}/check-in")]
public sealed class CheckInPasajerosController(ApplicationDbContext db) : ControllerBase
{
    public sealed class CambiarAsistenciaDto
    {
        public bool Presente { get; set; }
    }

    [HttpPut("pasajeros/{pasajeroId:int}")]
    public async Task<IActionResult> Actualizar(
        int tourId, int pasajeroId, CambiarAsistenciaDto dto, CancellationToken ct)
    {
        if (tourId <= 0 || pasajeroId <= 0) return NotFound();

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Se adquiere el mismo bloqueo por tour utilizado en el cierre operativo:
        // ningún check-in puede intercalarse con el recuento final.
        var existe = await db.Tours
            .FromSqlInterpolated($"SELECT * FROM dbo.Tours WITH (UPDLOCK, HOLDLOCK) WHERE Id = {tourId}")
            .AsNoTracking().AnyAsync(ct);
        if (!existe) return NotFound();
        if (await db.CierresOperativosTour.AnyAsync(x => x.TourId == tourId, ct))
            return Conflict("La excursión ya fue cerrada operativamente. No se puede cambiar el check-in.");

        // El ID del pasajero no basta: verificar pertenencia al tour y reserva confirmada.
        var info = await db.PasajerosReserva.AsNoTracking()
            .Where(p => p.Id == pasajeroId && p.Reserva.TourId == tourId)
            .Select(p => new
            {
                p.Id, p.ReservaId, p.LlegadaUtc,
                Estado = p.Reserva.Estado,
                Fecha = p.Reserva.Tour != null ? p.Reserva.Tour.Fecha : DateTime.MinValue,
                Hora = p.Reserva.Tour != null ? p.Reserva.Tour.Hora : TimeSpan.Zero
            }).SingleOrDefaultAsync(ct);

        if (info is null) return NotFound();
        if (info.Estado != "Confirmada")
            return Conflict("Solo se puede registrar llegada de pasajeros en reservas confirmadas.");
        if (DateTime.Now >= info.Fecha.Date.Add(info.Hora))
            return Conflict("La salida del tour ya comenzó. El check-in está cerrado.");

        var usuario = (User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "Personal").Trim();
        if (usuario.Length > 100) usuario = usuario[..100];

        // Actualización condicional atómica: dos operadores no pueden marcar
        // la misma llegada dos veces ni borrar un registro recién creado.
        var filas = dto.Presente
            ? await db.PasajerosReserva
                .Where(p => p.Id == pasajeroId && p.ReservaId == info.ReservaId &&
                    p.LlegadaUtc == null && p.Reserva.Estado == "Confirmada" &&
                    p.Reserva.Tour != null &&
                    (p.Reserva.Tour.Fecha > DateTime.Now.Date ||
                     (p.Reserva.Tour.Fecha == DateTime.Now.Date &&
                      p.Reserva.Tour.Hora > DateTime.Now.TimeOfDay)))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.LlegadaUtc, DateTime.UtcNow)
                    .SetProperty(p => p.RegistradoPor, usuario), ct)
            : await db.PasajerosReserva
                .Where(p => p.Id == pasajeroId && p.ReservaId == info.ReservaId &&
                    p.LlegadaUtc != null && p.Reserva.Estado == "Confirmada" &&
                    p.Reserva.Tour != null &&
                    (p.Reserva.Tour.Fecha > DateTime.Now.Date ||
                     (p.Reserva.Tour.Fecha == DateTime.Now.Date &&
                      p.Reserva.Tour.Hora > DateTime.Now.TimeOfDay)))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.LlegadaUtc, (DateTime?)null)
                    .SetProperty(p => p.RegistradoPor, (string?)null), ct);

        if (filas == 1)
        {
            await tx.CommitAsync(ct);
            return NoContent();
        }

        // Idempotente para estados ya alcanzados, sin duplicar registros.
        var actual = await db.PasajerosReserva.AsNoTracking()
            .Where(p => p.Id == pasajeroId && p.Reserva.TourId == tourId)
            .Select(p => new {
                p.LlegadaUtc, Estado = p.Reserva.Estado,
                Fecha = p.Reserva.Tour != null ? p.Reserva.Tour.Fecha : DateTime.MinValue,
                Hora = p.Reserva.Tour != null ? p.Reserva.Tour.Hora : TimeSpan.Zero
            }).SingleOrDefaultAsync(ct);
        if (actual is null) return NotFound();
        if (actual.Estado != "Confirmada" ||
            DateTime.Now >= actual.Fecha.Date.Add(actual.Hora))
            return Conflict("La reserva cambió de estado o la salida comenzó.");
        if ((actual.LlegadaUtc is not null) == dto.Presente)
        {
            await tx.CommitAsync(ct);
            return NoContent();
        }
        return Conflict("El estado cambió durante la operación. Actualiza el manifiesto.");
    }
}
