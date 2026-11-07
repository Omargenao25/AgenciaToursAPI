using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TourSmart.Web.Models;
using TourSmart.Web.Services;

namespace TourSmart.Web.Controllers;

[Route("cuenta")]
public sealed class CuentaController(IAuthApiClient authApi, IMemoryCache cache) : Controller
{
    [HttpGet("recuperar")]
    [AllowAnonymous]
    public IActionResult Recuperar() => View(new SolicitarRecuperacionViewModel());

    [HttpPost("recuperar")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recuperar(SolicitarRecuperacionViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var estado = await authApi.SolicitarRecuperacionAsync(model.Correo, ct);
            if (estado == System.Net.HttpStatusCode.OK)
            {
                // Respuesta deliberadamente idéntica para cuentas existentes o inexistentes.
                TempData["Recuperacion"] = "Si existe una cuenta de viajero con ese correo, recibirás instrucciones de recuperación.";
                return RedirectToAction(nameof(Recuperar));
            }
            ModelState.AddModelError("", "No se pudo procesar la solicitud. Intenta nuevamente.");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "No se pudo conectar con la API. Comprueba que esté encendida.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            ModelState.AddModelError("", "La solicitud tardó demasiado. Intenta de nuevo más tarde.");
        }
        return View(model);
    }

    [HttpGet("restablecer")]
    [AllowAnonymous]
    public IActionResult Restablecer(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length != 64)
            return View("EnlaceInvalido");
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        return View(new RestablecerContrasenaViewModel { Token = token });
    }

    [HttpPost("restablecer")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restablecer(RestablecerContrasenaViewModel model, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (string.IsNullOrWhiteSpace(model.Token) || model.Token.Length != 64)
            return View("EnlaceInvalido");
        if (!ModelState.IsValid) return View(model);

        try
        {
            var estado = await authApi.RestablecerAsync(model.Token, model.ContrasenaNueva, ct);
            if (estado == System.Net.HttpStatusCode.NoContent)
            {
                TempData["ContrasenaActualizada"] = "Tu contraseña fue restablecida. Inicia sesión con la nueva contraseña.";
                return RedirectToAction(nameof(Acceder));
            }
            ModelState.AddModelError("", estado == System.Net.HttpStatusCode.BadRequest
                ? "El enlace no es válido, expiró o la nueva contraseña no cumple los requisitos."
                : "No pudimos restablecer la contraseña. Intenta solicitar otro enlace.");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "No se pudo contactar con la API.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            ModelState.AddModelError("", "La solicitud tardó demasiado. Intenta iniciar sesión antes de repetir.");
        }

        // No devolver contraseñas al navegador después de un fallo.
        model.ContrasenaNueva = "";
        model.ConfirmarContrasena = "";
        ModelState.Remove(nameof(model.ContrasenaNueva));
        ModelState.Remove(nameof(model.ConfirmarContrasena));
        return View(model);
    }

    [HttpGet("acceder")]
    [AllowAnonymous]
    public IActionResult Acceder(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Panel");

        return View(new LoginViewModel { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
    }

    [HttpPost("acceder")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Acceder(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var login = await authApi.LoginAsync(model.Correo, model.Contrasena, cancellationToken);
            if (login is null)
            {
                ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(login.Token) ||
                login.Rol is not ("Administrador" or "Operador" or "Cliente"))
            {
                ModelState.AddModelError("", "No se pudo validar la respuesta del servidor.");
                return View(model);
            }

            var expirationUtc = login.Expiracion.Kind switch
            {
                DateTimeKind.Utc => login.Expiracion,
                DateTimeKind.Local => login.Expiracion.ToUniversalTime(),
                _ => DateTime.SpecifyKind(login.Expiracion, DateTimeKind.Utc)
            };
            if (expirationUtc <= DateTime.UtcNow)
            {
                ModelState.AddModelError("", "La sesión recibida ya expiró.");
                return View(model);
            }

            // El JWT queda exclusivamente en memoria del servidor; la cookie contiene
            // solo un identificador aleatorio y claims, nunca la contraseña ni el token.
            var sessionId = Guid.NewGuid().ToString("N");
            var expiration = new DateTimeOffset(expirationUtc);
            cache.Set("jwt:" + sessionId, login.Token, expiration);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, login.Nombre),
                new Claim(ClaimTypes.Role, login.Rol),
                new Claim("SessionId", sessionId)
            };
            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = false, ExpiresUtc = expiration });

            if (Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl);
            return RedirectToAction("Index", "Panel");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "No se pudo contactar con la API. Comprueba que está encendida.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            ModelState.AddModelError("", "La API tardó demasiado en responder. Inténtalo de nuevo.");
        }

        return View(model);
    }

    [HttpPost("salir")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        var sessionId = User.FindFirstValue("SessionId");
        if (!string.IsNullOrEmpty(sessionId)) cache.Remove("jwt:" + sessionId);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("denegado")]
    public IActionResult Denegado() => View();
}
