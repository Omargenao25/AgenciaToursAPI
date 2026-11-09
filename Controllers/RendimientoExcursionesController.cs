using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers;

[ApiController]
[Authorize(Roles = Roles.Administrador)]
[Route("api/reportes/rendimiento-excursiones")]
public sealed class RendimientoExcursionesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obtener(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] int pagina = 1, CancellationToken ct = default)
    {
        var hoy = DateTime.UtcNow.Date;
        var inicio = (desde ?? hoy.AddDays(-90)).Date;
        var fin = (hasta ?? hoy).Date;
        if (inicio.Year < 2000 || fin < inicio || fin >= DateTime.MaxValue.Date ||
            (fin - inicio).TotalDays > 366 || pagina < 1 || pagina > 100000)
            return BadRequest("Selecciona fechas UTC válidas, hasta 366 días, y una página positiva.");

        var exclusivo = fin.AddDays(1);
        const int tamano = 25;
        var cierres = db.CierresOperativosTour.AsNoTracking()
            .Where(c => c.FechaCierreUtc >= inicio && c.FechaCierreUtc < exclusivo);

        // Todos los KPI se agregan en SQL, sin depender de la página visible.
        // La capacidad del tour queda inmovilizada tras su cierre operativo.
        var total = await cierres.GroupBy(c => 1).Select(g => new
        {
            Excursiones = g.Count(),
            Capacidad = g.Sum(c => (int?)c.Tour.CapacidadMaxima) ?? 0,
            Confirmados = g.Sum(c => (int?)c.PlazasConfirmadas) ?? 0,
            Presentes = g.Sum(c => (int?)c.PasajerosPresentes) ?? 0,
            Ausentes = g.Sum(c => (int?)c.PasajerosAusentes) ?? 0,
            Incidencias = g.Count(c => c.Incidencias != null && c.Incidencias != "")
        }).SingleOrDefaultAsync(ct);

        var filas = await cierres
            .OrderByDescending(c => c.FechaCierreUtc).ThenByDescending(c => c.Id)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(c => new RendimientoExcursionFilaDto
            {
                TourId = c.TourId,
                Tour = c.Tour.Nombre,
                FechaSalida = c.Tour.Fecha,
                Capacidad = c.Tour.CapacidadMaxima,
                Confirmados = c.PlazasConfirmadas,
                Presentes = c.PasajerosPresentes,
                Ausentes = c.PasajerosAusentes,
                TieneIncidencias = c.Incidencias != null && c.Incidencias != ""
            }).ToListAsync(ct);

        Response.Headers.CacheControl = "private, no-store";
        return Ok(new RendimientoExcursionesDto
        {
            DesdeUtc = inicio, HastaExclusivoUtc = exclusivo,
            Pagina = pagina, TamanoPagina = tamano,
            TotalExcursiones = total?.Excursiones ?? 0,
            CapacidadTotal = total?.Capacidad ?? 0,
            TotalConfirmados = total?.Confirmados ?? 0,
            TotalPresentes = total?.Presentes ?? 0,
            TotalAusentes = total?.Ausentes ?? 0,
            TotalConIncidencias = total?.Incidencias ?? 0,
            Excursiones = filas
        });
    }
}
