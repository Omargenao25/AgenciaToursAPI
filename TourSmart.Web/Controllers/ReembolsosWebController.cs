using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using TourSmart.Web.Models;
using TourSmart.Web.Services;

namespace TourSmart.Web.Controllers;

[Authorize(Roles = "Administrador,Operador")]
[Route("panel/reembolsos")]
public sealed class ReembolsosWebController(IReservasAdminClient api, IMemoryCache cache) : Controller
{
    private string? Token()
    {
        var sid = User.FindFirstValue("SessionId");
        return sid is not null && cache.TryGetValue("jwt:" + sid, out object? value)
            ? value as string : null;
    }

    private async Task Cargar(RegistrarReembolsoViewModel model, string jwt, CancellationToken ct)
    {
        model.Resumen = await api.EstadoPagosAsync(jwt, model.ReservaId, ct);
        model.Reembolsos = await api.ReembolsosAsync(jwt, model.ReservaId, ct);
        if (User.IsInRole("Administrador"))
        {
            var catalogos = await api.CatalogosAsync(jwt, ct);
            model.Metodos = catalogos.Metodos
                .Select(x => new SelectListItem(x.Nombre, x.Id.ToString())).ToList();
        }
    }

    [HttpGet("{reservaId:int}")]
    public async Task<IActionResult> Detalle(int reservaId, CancellationToken ct)
    {
        if (reservaId <= 0) return NotFound();
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            var model = new RegistrarReembolsoViewModel { ReservaId = reservaId };
            await Cargar(model, jwt, ct);
            return View(model);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No fue posible consultar el historial de reembolsos.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado.";
        }
        return RedirectToAction("Index", "ReservasWeb");
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("{reservaId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(int reservaId, RegistrarReembolsoViewModel model, CancellationToken ct)
    {
        if (reservaId <= 0 || reservaId != model.ReservaId) return BadRequest();
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            await Cargar(model, jwt, ct);
            if (model.Resumen.EstadoReserva == "Cancelada")
                ModelState.AddModelError("", "La reserva está cancelada.");
            if (model.Monto > model.DisponibleParaDevolver)
                ModelState.AddModelError(nameof(model.Monto), "El importe supera lo pendiente de devolver.");
            if (!model.Metodos.Any(m => m.Value == model.MetodoPagoId.ToString()))
                ModelState.AddModelError(nameof(model.MetodoPagoId), "Método de devolución no válido.");

            if (ModelState.IsValid)
            {
                var (estado, mensaje) = await api.RegistrarReembolsoAsync(jwt, model, ct);
                if (estado is HttpStatusCode.Created or HttpStatusCode.OK)
                {
                    TempData["Exito"] = estado == HttpStatusCode.OK
                        ? "Esta devolución ya estaba registrada; no se duplicó."
                        : "Devolución registrada. Verifica el historial y la conciliación.";
                    return RedirectToAction(nameof(Detalle), new { reservaId });
                }

                ModelState.AddModelError("", estado switch
                {
                    HttpStatusCode.Conflict => "No se pudo registrar: revisa el importe disponible, el estado de la reserva y la referencia de operación.",
                    HttpStatusCode.Forbidden => "Solo Administrador puede registrar devoluciones.",
                    HttpStatusCode.BadRequest => "Revisa el importe, método y motivo.",
                    _ => $"La API no pudo registrar la devolución (HTTP {(int)estado})."
                });
            }
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "No se pudo contactar con la API. Verifica el historial antes de intentar de nuevo.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            ModelState.AddModelError("", "La solicitud tardó demasiado. Revisa el historial antes de reintentar.");
        }
        return View("Detalle", model);
    }
}
