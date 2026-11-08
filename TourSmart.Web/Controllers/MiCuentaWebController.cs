using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

    [HttpGet("seguridad")]
    [Authorize(Roles = "Cliente")]
    public IActionResult Seguridad() => View(new CambiarContrasenaViewModel());

    [HttpPost("seguridad")]
    [Authorize(Roles = "Cliente")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Seguridad(CambiarContrasenaViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt))
            return RedirectToAction("Acceder", "Cuenta");

        try
        {
            var (estado, mensaje) = await cuentaApi.CambiarContrasenaAsync(jwt, model, ct);
            if (estado == HttpStatusCode.NoContent)
            {
                var sessionId = User.FindFirstValue("SessionId");
                if (!string.IsNullOrWhiteSpace(sessionId)) cache.Remove("jwt:" + sessionId);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["ContrasenaActualizada"] = "Contraseña actualizada. Inicia sesión de nuevo con tu nueva contraseña.";
                return RedirectToAction("Acceder", "Cuenta");
            }

            ModelState.AddModelError("", estado switch
            {
                HttpStatusCode.BadRequest when mensaje.Contains("actual es incorrecta", StringComparison.OrdinalIgnoreCase)
                    => "La contraseña actual es incorrecta.",
                HttpStatusCode.BadRequest => "La nueva contraseña debe ser diferente y contener al menos 10 caracteres, mayúscula, minúscula, número y símbolo.",
                HttpStatusCode.Conflict => "Tu cuenta cambió durante la operación. Inicia sesión nuevamente.",
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Tu sesión ya no está autorizada. Vuelve a iniciar sesión.",
                _ => $"No se pudo cambiar la contraseña (HTTP {(int)estado})."
            });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "No se pudo contactar con la API. Intenta iniciar sesión antes de repetir el cambio.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            ModelState.AddModelError("", "La solicitud tardó demasiado. Verifica si la contraseña ya cambió antes de repetir.");
        }

        // Jamás devolver las contraseñas previamente escritas en el HTML.
        model.ContrasenaActual = "";
        model.ContrasenaNueva = "";
        model.ConfirmarContrasena = "";
        ModelState.Remove(nameof(model.ContrasenaActual));
        ModelState.Remove(nameof(model.ContrasenaNueva));
        ModelState.Remove(nameof(model.ConfirmarContrasena));
        return View(model);
    }

    [HttpGet("perfil")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Perfil(CancellationToken ct)
    {
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            return View(await cuentaApi.PerfilAsync(jwt, ct));
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = ex.StatusCode == HttpStatusCode.Conflict
                ? "Tu cuenta todavía no tiene una ficha de cliente vinculada."
                : "No se pudo cargar tu perfil. Comprueba la conexión con la API.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado al cargar tu perfil.";
        }
        return RedirectToAction("Index", "Panel");
    }

    [HttpPost("perfil")]
    [Authorize(Roles = "Cliente")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(MiPerfilViewModel model, CancellationToken ct)
    {
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");

        model.Nombre = model.Nombre?.Trim() ?? "";
        model.Apellido = model.Apellido?.Trim() ?? "";
        model.Telefono = model.Telefono?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(model.Nombre))
            ModelState.AddModelError(nameof(model.Nombre), "El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(model.Apellido))
            ModelState.AddModelError(nameof(model.Apellido), "El apellido es obligatorio.");
        if (string.IsNullOrWhiteSpace(model.Telefono))
            ModelState.AddModelError(nameof(model.Telefono), "El teléfono es obligatorio.");

        if (ModelState.IsValid)
        {
            try
            {
                var estado = await cuentaApi.ActualizarPerfilAsync(jwt, model, ct);
                if (estado == HttpStatusCode.NoContent)
                {
                    TempData["Exito"] = "Tus datos personales fueron actualizados correctamente.";
                    return RedirectToAction(nameof(Perfil));
                }
                ModelState.AddModelError("", estado switch
                {
                    HttpStatusCode.BadRequest => "La API rechazó los datos. Revisa los campos.",
                    HttpStatusCode.Conflict => "Esta cuenta no tiene una ficha verificada vinculada.",
                    HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "Tu sesión no permite modificar este perfil.",
                    _ => $"No se pudo actualizar el perfil (HTTP {(int)estado})."
                });
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "No se pudo contactar con la API. Comprueba si se guardaron los cambios antes de reintentar.");
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                ModelState.AddModelError("", "La API tardó demasiado. Revisa el perfil antes de reintentar.");
            }
        }

        // No confiar en los campos de solo lectura enviados por el navegador:
        // siempre recuperarlos de la API autenticada.
        try
        {
            var actual = await cuentaApi.PerfilAsync(jwt, ct);
            model.Correo = actual.Correo;
            model.TipoDocumento = actual.TipoDocumento;
            model.NumeroDocumento = actual.NumeroDocumento;
            model.PaisEmisorDocumento = actual.PaisEmisorDocumento;
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "No se pudieron recuperar los datos de identificación.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            ModelState.AddModelError("", "La API tardó demasiado al cargar la identificación.");
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

    [HttpGet("reservas/{id:int}/comprobante")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Comprobante(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            var comprobante = await cuentaApi.ComprobanteAsync(jwt, id, ct);
            if (comprobante is null) return NotFound();
            Response.Headers.CacheControl = "private, no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return View(comprobante);
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = ex.StatusCode == HttpStatusCode.Unauthorized
                ? "Tu sesión expiró. Inicia sesión nuevamente."
                : "No pudimos cargar el comprobante. Intenta nuevamente.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La consulta del comprobante tardó demasiado.";
        }
        return RedirectToAction(nameof(MisReservas));
    }

    [HttpPost("reservas/{id:int}/cancelar")]
    [Authorize(Roles = "Cliente")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarMiReserva(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();

        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt))
            return RedirectToAction("Acceder", "Cuenta");

        try
        {
            var (estado, _) = await cuentaApi.CancelarAsync(jwt, id, ct);
            if (estado == HttpStatusCode.NoContent)
            {
                TempData["Exito"] = $"La reserva #{id} se canceló correctamente. Sus plazas vuelven a estar disponibles.";
                return RedirectToAction(nameof(MisReservas));
            }

            TempData["Error"] = estado switch
            {
                HttpStatusCode.NotFound => "No se encontró esta reserva entre tus reservas.",
                HttpStatusCode.Conflict => "No se pudo cancelar: la reserva ya está cancelada, el tour comenzó o tu cuenta necesita vinculación.",
                HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "Tu sesión no permite cancelar esta reserva.",
                _ => $"No se pudo cancelar la reserva (HTTP {(int)estado})."
            };
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No fue posible contactar con la API. Verifica el estado de la reserva antes de reintentar.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La operación tardó demasiado. Revisa el estado de la reserva antes de reintentar.";
        }

        return RedirectToAction(nameof(MisReservas));
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

            if (model.Pasajeros is null || model.Pasajeros.Count != model.CantidadPersonas ||
                model.Pasajeros.Any(n => string.IsNullOrWhiteSpace(n) || n.Trim().Length < 3 || n.Trim().Length > 120))
                ModelState.AddModelError(nameof(model.Pasajeros),
                    "Introduce un nombre válido (3-120 caracteres) para cada plaza solicitada.");

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
