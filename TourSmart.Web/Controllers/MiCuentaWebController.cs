using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using TourSmart.Web.Models;
using TourSmart.Web.Services;

namespace TourSmart.Web.Controllers;

[Route("viajero")]
public sealed class MiCuentaWebController(
    IMiCuentaApiClient cuentaApi,
    IToursApiClient toursApi,
    IMemoryCache cache) : Controller
{
    private string? Token()
    {
        var id = User.FindFirstValue("SessionId");
        return id is not null && cache.TryGetValue("jwt:" + id, out object? value)
            ? value as string : null;
    }

    [HttpGet("registro")]
    [AllowAnonymous]
    public IActionResult Registro()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Panel");
        return View(new RegistroViajeroViewModel());
    }

    [HttpPost("registro")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroViajeroViewModel model, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Panel");
        if (!ModelState.IsValid) return View(model);

        try
        {
            var status = await cuentaApi.RegistrarAsync(model, ct);
            if (status == HttpStatusCode.Created)
            {
                TempData["RegistroExitoso"] = "Cuenta creada correctamente. Ya puedes iniciar sesión.";
                return RedirectToAction("Acceder", "Cuenta");
            }
            ModelState.AddModelError("", status switch
            {
                HttpStatusCode.Conflict => "El correo o documento ya está registrado. Contacta a la agencia para verificar una cuenta existente.",
                HttpStatusCode.BadRequest => "Revisa los datos ingresados.",
                _ => "No se pudo completar el registro (HTTP " + (int)status + ")."
            });
        }
        catch (HttpRequestException) { ModelState.AddModelError("", "No se pudo conectar con la API."); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            ModelState.AddModelError("", "El registro tardó demasiado. Revisa si ya existe tu cuenta antes de intentar de nuevo.");
        }
        return View(model);
    }

    [HttpGet("reservas")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> MisReservas(CancellationToken ct)
    {
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            var reservas = await cuentaApi.MisReservasAsync(jwt, ct);
            return View(reservas);
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = ex.StatusCode == HttpStatusCode.Conflict
                ? "Tu cuenta no está vinculada a una ficha de cliente verificada. Contacta a la agencia."
                : "No pudimos cargar tus reservas. Verifica tu sesión y vuelve a intentarlo.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado al consultar tus reservas.";
        }
        return View(Array.Empty<ReservaPanel>());
    }

    private async Task<bool> CargarReserva(MiReservaFormulario model, string jwt, CancellationToken ct)
    {
        model.Tour = await toursApi.GetTourAsync(model.TourId, ct);
        if (model.Tour is null) return false;
        var metodos = await cuentaApi.MetodosAsync(jwt, ct);
        model.Metodos = metodos.Select(m => new SelectListItem(m.Nombre, m.Id.ToString())).ToList();
        return true;
    }

    [HttpGet("reservar/{tourId:int}")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Reservar(int tourId, CancellationToken ct)
    {
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");
        var model = new MiReservaFormulario { TourId = tourId };
        try
        {
            if (!await CargarReserva(model, jwt, ct)) return NotFound();
            if (!model.Tour!.Reservable || model.Tour.Fecha.Date.Add(model.Tour.Hora) <= DateTime.Now)
            {
                TempData["Error"] = "El tour seleccionado ya no tiene disponibilidad.";
                return RedirectToAction("Index", "Tours");
            }
            return View(model);
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = ex.StatusCode == HttpStatusCode.Conflict
                ? "Tu cuenta no está vinculada a una ficha de cliente verificada."
                : "No fue posible cargar el formulario de reserva.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { TempData["Error"] = "La API tardó demasiado en responder."; }
        return RedirectToAction("Index", "Tours");
    }

    [HttpPost("reservar/{tourId:int}")]
    [Authorize(Roles = "Cliente")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reservar(int tourId, MiReservaFormulario model, CancellationToken ct)
    {
        if (tourId <= 0 || model.TourId != tourId) return BadRequest();
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            if (!await CargarReserva(model, jwt, ct)) return NotFound();
            if (!model.Tour!.Reservable || model.Tour.Fecha.Date.Add(model.Tour.Hora) <= DateTime.Now)
                ModelState.AddModelError("", "Este tour ya no está disponible.");
            if (model.CantidadPersonas > model.Tour.CuposDisponibles)
                ModelState.AddModelError(nameof(model.CantidadPersonas), "No hay suficientes plazas disponibles.");
            if (!model.Metodos.Any(x => x.Value == model.MetodoPagoId.ToString()))
                ModelState.AddModelError(nameof(model.MetodoPagoId), "Selecciona un método de pago válido.");

            if (ModelState.IsValid)
            {
                var (status, _) = await cuentaApi.ReservarAsync(jwt, model, ct);
                if (status == HttpStatusCode.Created)
                {
                    TempData["Exito"] = "Tu reserva fue creada en estado Pendiente. ¡Gracias por elegir TourSmart RD!";
                    return RedirectToAction(nameof(MisReservas));
                }
                ModelState.AddModelError("", status switch
                {
                    HttpStatusCode.Conflict => "No se pudo reservar: cupos insuficientes, tour iniciado o cuenta pendiente de vincular.",
                    HttpStatusCode.BadRequest => "Los datos de la reserva no son válidos.",
                    HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "Tu sesión no autoriza esta operación.",
                    _ => "No se pudo registrar la reserva (HTTP " + (int)status + ")."
                });
            }
        }
        catch (HttpRequestException) { ModelState.AddModelError("", "No fue posible contactar con la API."); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            ModelState.AddModelError("", "La solicitud tardó demasiado; consulta Mis reservas antes de intentar de nuevo.");
        }
        return View(model);
    }
}
