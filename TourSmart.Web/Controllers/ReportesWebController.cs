using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TourSmart.Web.Models;
using TourSmart.Web.Services;

namespace TourSmart.Web.Controllers;

[Authorize(Roles = "Administrador")]
[Route("panel/reportes")]
public sealed class ReportesWebController(IReservasAdminClient api, IMemoryCache cache) : Controller
{
    [HttpGet("cuentas-por-cobrar")]
    public async Task<IActionResult> CuentasPorCobrar(CancellationToken ct)
    {
        var sid = User.FindFirstValue("SessionId");
        if (sid is null || !cache.TryGetValue("jwt:" + sid, out object? value) ||
            value is not string jwt || string.IsNullOrWhiteSpace(jwt))
            return RedirectToAction("Acceder", "Cuenta");

        try
        {
            var modelo = await api.CuentasPorCobrarAsync(jwt, ct);
            Response.Headers.CacheControl = "private, no-store";
            return View(modelo);
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized
                ? "No tienes autorización para consultar los saldos."
                : "No se pudo consultar el reporte. Comprueba que la API está encendida.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado. Intenta nuevamente.";
        }
        return View(new CuentasPorCobrarViewModel());
    }

    [HttpGet("finanzas/exportar-csv")]
    public async Task<IActionResult> ExportarCsv(DateTime? desde, DateTime? hasta, CancellationToken ct)
    {
        var hoy = DateTime.UtcNow.Date;
        var inicio = (desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
        var fin = (hasta ?? hoy).Date;

        if (inicio.Year < 2000 || fin < inicio || fin >= DateTime.MaxValue.Date ||
            (fin - inicio).TotalDays > 366)
            return BadRequest("Selecciona un período válido de hasta 366 días, a partir del año 2000.");

        var sid = User.FindFirstValue("SessionId");
        if (sid is null || !cache.TryGetValue("jwt:" + sid, out object? value) ||
            value is not string jwt || string.IsNullOrWhiteSpace(jwt))
            return RedirectToAction("Acceder", "Cuenta");

        try
        {
            using var respuesta = await api.ExportarFinanzasCsvAsync(jwt, inicio, fin, ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                TempData["Error"] = respuesta.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized
                    ? "La API no autorizó la exportación financiera."
                    : "La API no pudo generar el archivo CSV.";
                return RedirectToAction(nameof(Finanzas), new { desde = inicio.ToString("yyyy-MM-dd"), hasta = fin.ToString("yyyy-MM-dd") });
            }

            Response.ContentType = "text/csv; charset=utf-8";
            Response.Headers["Cache-Control"] = "private, no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Content-Disposition"] =
                $"attachment; filename=\"TourSmart_Finanzas_{inicio:yyyyMMdd}_{fin:yyyyMMdd}.csv\"";
            await using var archivo = await respuesta.Content.ReadAsStreamAsync(ct);
            await archivo.CopyToAsync(Response.Body, ct);
            return new EmptyResult();
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No se pudo descargar el CSV. Comprueba la conexión con la API.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La descarga tardó demasiado. Prueba con un intervalo más corto.";
        }

        return RedirectToAction(nameof(Finanzas), new {
            desde = inicio.ToString("yyyy-MM-dd"),
            hasta = fin.ToString("yyyy-MM-dd")
        });
    }

    [HttpGet("finanzas")]
    public async Task<IActionResult> Finanzas(DateTime? desde, DateTime? hasta, CancellationToken ct)
    {
        var hoy = DateTime.UtcNow.Date;
        var inicio = (desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
        var fin = (hasta ?? hoy).Date;

        if (inicio.Year < 2000 || fin < inicio || fin >= DateTime.MaxValue.Date ||
            (fin - inicio).TotalDays > 366)
        {
            ModelState.AddModelError("", "Selecciona un período válido de hasta 366 días.");
            return View(new ReporteFinancieroViewModel {
                DesdeUtc = inicio,
                HastaExclusivoUtc = fin < DateTime.MaxValue.Date ? fin.AddDays(1) : fin
            });
        }

        var sid = User.FindFirstValue("SessionId");
        if (sid is null || !cache.TryGetValue("jwt:" + sid, out object? value) ||
            value is not string jwt || string.IsNullOrWhiteSpace(jwt))
            return RedirectToAction("Acceder", "Cuenta");

        try
        {
            var model = await api.ReporteFinancieroAsync(jwt, inicio, fin, ct);
            Response.Headers.CacheControl = "private, no-store";
            return View(model);
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized
                ? "La API no autorizó consultar datos financieros."
                : "No se pudo consultar el reporte financiero. Revisa la conexión con la API.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado en generar el reporte.";
        }

        return View(new ReporteFinancieroViewModel {
            DesdeUtc = inicio, HastaExclusivoUtc = fin.AddDays(1)
        });
    }
}
