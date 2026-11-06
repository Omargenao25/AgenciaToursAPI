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
