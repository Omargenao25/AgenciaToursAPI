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
    [Authorize(Roles = "Administrador,Operador")]
    [HttpPost("reservas/{id:int}/{accion}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, string accion, CancellationToken cancellationToken)
    {
        if (id <= 0 || accion is not ("confirmar" or "cancelar"))
            return BadRequest();

        var sessionId = User.FindFirstValue("SessionId");
        if (string.IsNullOrWhiteSpace(sessionId) ||
            !cache.TryGetValue("jwt:" + sessionId, out object? cachedToken) ||
            cachedToken is not string jwt || string.IsNullOrWhiteSpace(jwt))
        {
            TempData["Error"] = "La sesión expiró. Inicia sesión nuevamente.";
            return RedirectToAction("Acceder", "Cuenta");
        }

        try
        {
            var status = await reservasApi.CambiarEstadoAsync(jwt, id, accion, cancellationToken);
            switch (status)
            {
                case HttpStatusCode.NoContent:
                    TempData["Exito"] = accion == "confirmar"
                        ? $"La reserva #{id} fue confirmada."
                        : $"La reserva #{id} fue cancelada y sus cupos fueron liberados.";
                    break;
                case HttpStatusCode.Conflict:
                    TempData["Error"] = "La operación no es válida para el estado o la fecha actual de esta reserva.";
                    break;
                case HttpStatusCode.NotFound:
                    TempData["Error"] = "La reserva solicitada no existe.";
                    break;
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    TempData["Error"] = "La API no autorizó la operación. Vuelve a iniciar sesión y revisa tus permisos.";
                    break;
                default:
                    TempData["Error"] = "No se pudo actualizar la reserva. Revisa el estado de la API.";
                    break;
            }
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No se pudo conectar con la API. Comprueba que está funcionando.";
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado en responder. Consulta el estado de la reserva antes de repetir.";
        }

        return RedirectToAction(nameof(Index));
    }

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
