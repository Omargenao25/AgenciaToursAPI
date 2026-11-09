using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers;

[ApiController]
[Authorize(Roles = Roles.Administrador)]
[Route("api/reportes/historial-excursiones")]
public sealed class HistorialExcursionesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Consultar(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] int pagina = 1, CancellationToken ct = default)
    {
        // El período corresponde a FECHA DE CIERRE en UTC, no a fecha de salida.
        var hoy = DateTime.UtcNow.Date;
        var inicio = (desde ?? hoy.AddDays(-90)).Date;
        var fin = (hasta ?? hoy).Date;
        if (inicio.Year < 2000 || fin < inicio || fin >= DateTime.MaxValue.Date ||
            (fin - inicio).TotalDays > 366)
            return BadRequest("Selecciona fechas de cierre válidas (máximo 366 días).");
        if (pagina < 1 || pagina > 100000)
            return BadRequest("La página solicitada no es válida.");
        var exclusivo = fin.AddDays(1);
        const int tamanoPagina = 25;

        var consulta = db.CierresOperativosTour.AsNoTracking()
            .Where(c => c.FechaCierreUtc >= inicio && c.FechaCierreUtc < exclusivo);

        // Agregados exactos de todo el período, independientemente de la página.
        var resumen = await consulta.GroupBy(c => 1).Select(g => new
        {
            Total = g.Count(),
            Plazas = g.Sum(c => (int?)c.PlazasConfirmadas) ?? 0,
            Presentes = g.Sum(c => (int?)c.PasajerosPresentes) ?? 0,
            Ausentes = g.Sum(c => (int?)c.PasajerosAusentes) ?? 0,
            ConIncidencias = g.Count(c => c.Incidencias != null && c.Incidencias != "")
        }).SingleOrDefaultAsync(ct);

        var filas = await consulta
            .OrderByDescending(c => c.FechaCierreUtc).ThenByDescending(c => c.Id)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina)
            .Select(c => new HistorialExcursionFilaDto
            {
                TourId = c.TourId,
                Tour = c.Tour.Nombre,
                FechaSalida = c.Tour.Fecha,
                HoraSalida = c.Tour.Hora,
                Guia = c.Tour.Guia != null ? c.Tour.Guia.Nombre : "",
                Transporte = c.Tour.Transporte != null ? c.Tour.Transporte.Tipo : "",
                FechaCierreUtc = c.FechaCierreUtc,
                CerradoPor = c.CerradoPor,
                PlazasConfirmadas = c.PlazasConfirmadas,
                PasajerosPresentes = c.PasajerosPresentes,
                PasajerosAusentes = c.PasajerosAusentes,
                Incidencias = c.Incidencias
            })
            .ToListAsync(ct);

        Response.Headers.CacheControl = "private, no-store";
        return Ok(new HistorialExcursionesDto
        {
            DesdeUtc = inicio,
            HastaExclusivoUtc = exclusivo,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalCierres = resumen?.Total ?? 0,
            TotalPlazasConfirmadas = resumen?.Plazas ?? 0,
            TotalPresentes = resumen?.Presentes ?? 0,
            TotalAusentes = resumen?.Ausentes ?? 0,
            TotalConIncidencias = resumen?.ConIncidencias ?? 0,
            Cierres = filas
        });
    }
}
