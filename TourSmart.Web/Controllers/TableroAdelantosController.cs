using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TourSmart.Web.Models;

namespace TourSmart.Web.Controllers;

[Authorize(Roles = "Administrador")]
[Route("panel/finanzas/adelantos")]
public sealed class TableroAdelantosController(IHttpClientFactory factory, IMemoryCache cache) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, CancellationToken ct)
    {
        var hoy = DateTime.UtcNow.Date;
        var inicio = (desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
        var fin = (hasta ?? hoy).Date;
        var vacio = new TableroAdelantosViewModel
        {
            DesdeUtc = inicio,
            HastaExclusivoUtc = fin < DateTime.MaxValue.Date ? fin.AddDays(1) : fin
        };
        if (inicio.Year < 2000 || fin < inicio || fin >= DateTime.MaxValue.Date ||
            (fin - inicio).TotalDays > 366)
        {
            ModelState.AddModelError("", "Elige fechas UTC válidas (máximo 366 días).");
            return View(vacio);
        }

        var session = User.FindFirstValue("SessionId");
        if (session is null || !cache.TryGetValue("jwt:" + session, out object? value) ||
            value is not string jwt || string.IsNullOrWhiteSpace(jwt))
            return RedirectToAction("Acceder", "Cuenta");

        try
        {
            var api = factory.CreateClient("TourSmart.Sesion");
            var url = $"api/reportes/comprobantes?desde={inicio:yyyy-MM-dd}&hasta={fin:yyyy-MM-dd}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            using var resp = await api.SendAsync(req, ct);
            if (resp.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
            {
                TempData["Error"] = "No tienes permisos para consultar este reporte.";
                return View(vacio);
            }
            resp.EnsureSuccessStatusCode();
            var data = await resp.Content.ReadFromJsonAsync<TableroAdelantosViewModel>(cancellationToken: ct);
            if (data is null) throw new HttpRequestException("Respuesta vacía");
            Response.Headers.CacheControl = "private, no-store";
            return View(data);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "El tablero no pudo consultar la API. Verifica que esté iniciada.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado en responder.";
        }
        return View(vacio);
    }
}
