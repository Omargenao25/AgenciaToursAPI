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
