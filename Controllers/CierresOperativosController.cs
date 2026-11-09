using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AgenciaToursAPI.Controllers;

[ApiController]
[Authorize(Roles = Roles.Personal)]
[Route("api/tours/{tourId:int}/cierre-operativo")]
public sealed class CierresOperativosController(ApplicationDbContext db) : ControllerBase
{
    public sealed class RegistrarCierreDto
    {
        public bool Confirmar { get; set; }
        public string? Incidencias { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Obtener(int tourId, CancellationToken ct)
    {
        if (tourId <= 0) return NotFound();
        if (!await db.Tours.AsNoTracking().AnyAsync(t => t.Id == tourId, ct))
            return NotFound("El tour no existe.");

        var cierre = await db.CierresOperativosTour.AsNoTracking()
            .Where(c => c.TourId == tourId)
            .Select(c => new
            {
                c.TourId, c.FechaCierreUtc, c.CerradoPor,
                c.PlazasConfirmadas, c.PasajerosPresentes, c.PasajerosAusentes,
                c.Incidencias
            }).SingleOrDefaultAsync(ct);

        Response.Headers.CacheControl = "private, no-store";
        return Ok(new { Cerrado = cierre != null, Cierre = cierre });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Registrar(int tourId, RegistrarCierreDto dto, CancellationToken ct)
    {
        if (tourId <= 0) return NotFound();
        if (!dto.Confirmar)
            return BadRequest("Debes confirmar expresamente el cierre operativo.");
        var incidencias = (dto.Incidencias ?? "").Trim();
        if (incidencias.Length > 1000)
            return BadRequest("Las incidencias no pueden superar 1000 caracteres.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Mismo bloqueo que el check-in: impide que cambie la asistencia
        // mientras se toman los contadores definitivos.
        var tour = await db.Tours
            .FromSqlInterpolated($"SELECT * FROM dbo.Tours WITH (UPDLOCK, HOLDLOCK) WHERE Id = {tourId}")
            .AsNoTracking().SingleOrDefaultAsync(ct);
        if (tour is null) return NotFound();

        if (await db.CierresOperativosTour.AnyAsync(c => c.TourId == tourId, ct))
            return Conflict("La excursión ya tiene un cierre operativo. No se puede duplicar.");

        var confirmadas = await db.Reservas.AsNoTracking()
            .Where(r => r.TourId == tourId && r.Estado == "Confirmada")
            .Select(r => new
            {
                r.CantidadPersonas,
                Nombres = db.PasajerosReserva.Count(p => p.ReservaId == r.Id),
                Llegados = db.PasajerosReserva.Count(p =>
                    p.ReservaId == r.Id && p.LlegadaUtc != null)
            }).ToListAsync(ct);

        var plazas = confirmadas.Sum(r => r.CantidadPersonas);
        if (plazas <= 0)
            return Conflict("No hay pasajeros confirmados para registrar una salida.");
        if (confirmadas.Any(r => r.Nombres != r.CantidadPersonas))
            return Conflict("Hay reservas confirmadas sin todos sus nombres individuales. Completa el manifiesto.");

        var presentes = confirmadas.Sum(r => r.Llegados);
        if (presentes <= 0)
            return Conflict("No hay pasajeros marcados como presentes. Verifica el check-in antes de cerrar.");

        var usuario = (User.Identity?.Name ?? "Administrador").Trim();
        if (usuario.Length > 100) usuario = usuario[..100];

        var cierre = new CierreOperativoTour
        {
            TourId = tourId,
            FechaCierreUtc = DateTime.UtcNow,
            CerradoPor = usuario,
            PlazasConfirmadas = plazas,
            PasajerosPresentes = presentes,
            PasajerosAusentes = plazas - presentes,
            Incidencias = incidencias.Length == 0 ? null : incidencias
        };
        db.CierresOperativosTour.Add(cierre);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        Response.Headers.CacheControl = "private, no-store";
        return StatusCode(StatusCodes.Status201Created, new
        {
            cierre.TourId, cierre.FechaCierreUtc, cierre.CerradoPor,
            cierre.PlazasConfirmadas, cierre.PasajerosPresentes,
            cierre.PasajerosAusentes, cierre.Incidencias
        });
    }
}
