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
[Route("panel/pagos")]
public sealed class PagosWebController(
    IReservasAdminClient reservasApi,
    IMemoryCache cache) : Controller
{
    private string? Token()
    {
        var sessionId = User.FindFirstValue("SessionId");
        return sessionId is not null && cache.TryGetValue("jwt:" + sessionId, out object? value)
            ? value as string : null;
    }

    private async Task Cargar(RegistrarPagoViewModel model, string jwt, CancellationToken ct)
    {
        model.Resumen = await reservasApi.EstadoPagosAsync(jwt, model.ReservaId, ct);
        var catalogos = await reservasApi.CatalogosAsync(jwt, ct);
        model.Metodos = catalogos.Metodos
            .Select(m => new SelectListItem(m.Nombre, m.Id.ToString()))
            .ToList();
    }

    [HttpGet("{reservaId:int}")]
    public async Task<IActionResult> Detalle(int reservaId, CancellationToken ct)
    {
        if (reservaId <= 0) return NotFound();
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");

        var model = new RegistrarPagoViewModel { ReservaId = reservaId };
        try
        {
            await Cargar(model, jwt, ct);
            return View(model);
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = ex.StatusCode == HttpStatusCode.NotFound
                ? "La reserva solicitada no existe."
                : "No fue posible cargar los pagos de esta reserva.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado al consultar los pagos.";
        }
        return RedirectToAction("Index", "ReservasWeb");
    }

    [HttpPost("{reservaId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(int reservaId, RegistrarPagoViewModel model, CancellationToken ct)
    {
        if (reservaId <= 0 || reservaId != model.ReservaId) return BadRequest();
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");

        try
        {
            await Cargar(model, jwt, ct);
            if (model.Resumen.EstadoReserva == "Cancelada")
                ModelState.AddModelError("", "No se pueden registrar pagos en reservas canceladas.");
            if (model.Monto > model.Resumen.Saldo)
                ModelState.AddModelError(nameof(model.Monto), "El monto supera el saldo disponible.");
            if (!model.Metodos.Any(m => m.Value == model.MetodoPagoId.ToString()))
                ModelState.AddModelError(nameof(model.MetodoPagoId), "Selecciona un método de pago válido.");

            if (ModelState.IsValid)
            {
                var (status, mensaje) = await reservasApi.RegistrarPagoAsync(jwt, model, ct);
                if (status is HttpStatusCode.Created or HttpStatusCode.OK)
                {
                    TempData["Exito"] = status == HttpStatusCode.OK
                        ? "Esta operación ya estaba registrada. No se duplicó el pago."
                        : "Pago registrado correctamente. El saldo fue actualizado.";
                    return RedirectToAction(nameof(Detalle), new { reservaId });
                }
                ModelState.AddModelError("", status switch
                {
                    HttpStatusCode.Conflict => "No se pudo registrar: el importe supera el saldo, la reserva está cancelada o la operación se reutilizó con otros datos.",
                    HttpStatusCode.BadRequest => "Revisa el importe, el método de pago y los datos ingresados.",
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Tu sesión no está autorizada para registrar pagos.",
                    _ => $"No se pudo registrar el pago (HTTP {(int)status})."
                });
            }
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "No se pudo conectar con la API. Comprueba el historial antes de intentar de nuevo.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            ModelState.AddModelError("", "La solicitud tardó demasiado. Revisa el historial antes de intentar de nuevo.");
        }

        // Conserva ClaveOperacion para que un reintento no duplique el cobro.
        return View("Detalle", model);
    }
}
