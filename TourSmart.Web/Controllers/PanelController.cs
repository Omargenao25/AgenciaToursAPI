using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TourSmart.Web.Models;
using TourSmart.Web.Services;

namespace TourSmart.Web.Controllers;

[Authorize]
[Route("panel")]
public sealed class PanelController(
    IToursApiClient toursApi,
    IReservasAdminClient reservasApi,
    IMemoryCache cache) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var rol = User.FindFirstValue(ClaimTypes.Role) ?? "Cliente";
        var model = new DashboardViewModel { Rol = rol };

        if (rol is not ("Administrador" or "Operador"))
            return View(model);

        var sessionId = User.FindFirstValue("SessionId");
        if (string.IsNullOrWhiteSpace(sessionId) ||
            !cache.TryGetValue("jwt:" + sessionId, out object? sessionToken) ||
            sessionToken is not string jwt ||
            string.IsNullOrWhiteSpace(jwt))
        {
            return RedirectToAction("Acceder", "Cuenta");
        }

        try
        {
            model.Tours = await toursApi.GetToursAsync(cancellationToken);
            model.Reservas = await reservasApi.ListarAsync(jwt, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            model.ApiDisponible = false;
            model.Error = ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized
                ? "La API rechazó la autorización. Cierra sesión y vuelve a iniciar."
                : "No fue posible consultar los datos de la API. Verifica que esté encendida.";
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            model.ApiDisponible = false;
            model.Error = "La API tardó demasiado en responder. Vuelve a intentar.";
        }
        return View(model);
    }
}
