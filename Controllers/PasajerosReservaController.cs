using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Models;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AgenciaToursAPI.Controllers;

[ApiController]
[Authorize(Roles = Roles.Personal)]
[Route("api/reservas/{reservaId:int}/pasajeros")]
public sealed class PasajerosReservaController(ApplicationDbContext db) : ControllerBase
{
    public sealed class ActualizarPasajerosDto
    {
        public List<string> Pasajeros { get; set; } = new();
    }

    [HttpGet]
    public async Task<IActionResult> Listar(int reservaId, CancellationToken ct)
    {
        var reserva = await db.Reservas.AsNoTracking()
            .Where(r => r.Id == reservaId)
            .Select(r => new { r.Id, r.CantidadPersonas, r.Estado,
                Tour = r.Tour != null ? r.Tour.Nombre : "",
                InicioTour = r.Tour != null ? r.Tour.Fecha : DateTime.MinValue,
                Hora = r.Tour != null ? r.Tour.Hora : TimeSpan.Zero })
            .SingleOrDefaultAsync(ct);
        if (reserva is null) return NotFound();
        var nombres = await db.PasajerosReserva.AsNoTracking()
            .Where(p => p.ReservaId == reservaId)
            .OrderBy(p => p.Orden)
            .Select(p => p.NombreCompleto)
            .ToListAsync(ct);
        Response.Headers.CacheControl = "private, no-store";
        return Ok(new { reserva.Id, reserva.Tour, reserva.CantidadPersonas,
            reserva.Estado, reserva.InicioTour, reserva.Hora, Pasajeros = nombres });
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar(int reservaId, ActualizarPasajerosDto dto, CancellationToken ct)
    {
        if (reservaId <= 0) return NotFound();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var reserva = await db.Reservas
            .FromSqlInterpolated($"SELECT * FROM dbo.Reservas WITH (UPDLOCK, HOLDLOCK) WHERE Id = {reservaId}")
            .SingleOrDefaultAsync(ct);
        if (reserva is null) return NotFound();
        if (reserva.Estado == EstadosReserva.Cancelada)
            return Conflict("No se pueden modificar pasajeros de reservas canceladas.");

        var tour = await db.Tours.AsNoTracking().Where(t => t.Id == reserva.TourId)
            .Select(t => new { t.Fecha, t.Hora }).SingleOrDefaultAsync(ct);
        if (tour is null) return NotFound();
        if (DateTime.Now >= tour.Fecha.Date.Add(tour.Hora))
            return Conflict("No se puede modificar la lista después del inicio del tour.");

        if (!ValidacionPasajeros.IntentarNormalizar(reserva.CantidadPersonas,
            dto.Pasajeros, out var nuevos, out var error))
            return BadRequest(error);

        var anteriores = await db.PasajerosReserva
            .Where(p => p.ReservaId == reservaId).ToListAsync(ct);
        db.PasajerosReserva.RemoveRange(anteriores);
        // Una única SaveChanges efectúa el reemplazo en orden y evita colisiones
        // con el índice (ReservaId, Orden) durante un intento fallido.
        await db.SaveChangesAsync(ct);
        foreach (var pasajero in nuevos) pasajero.ReservaId = reservaId;
        db.PasajerosReserva.AddRange(nuevos);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return NoContent();
    }
}
