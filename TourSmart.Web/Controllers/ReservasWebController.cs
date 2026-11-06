using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TourSmart.Web.Models;
using TourSmart.Web.Services;

namespace TourSmart.Web.Controllers;

[Authorize(Roles = "Administrador,Operador")]
[Route("panel/reservas")]
public sealed class ReservasWebController(IReservasAdminClient reservasApi, IMemoryCache cache) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? busqueda, string? estado, int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        var model = new ReservasListadoViewModel
        {
            Busqueda = (busqueda ?? "").Trim().Length > 100 ? (busqueda ?? "").Trim()[..100] : (busqueda ?? "").Trim(),
            Estado = estado is "Pendiente" or "Confirmada" or "Cancelada" ? estado : "Todas"
        };

        var sessionId = User.FindFirstValue("SessionId");
        if (string.IsNullOrWhiteSpace(sessionId) ||
            !cache.TryGetValue("jwt:" + sessionId, out object? cached) ||
            cached is not string jwt || string.IsNullOrWhiteSpace(jwt))
            return RedirectToAction("Acceder", "Cuenta");

        try
        {
            var reservas = await reservasApi.ListarAsync(jwt, cancellationToken);
            var resultado = reservas.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(model.Busqueda))
            {
                resultado = resultado.Where(r =>
                    r.Cliente.Contains(model.Busqueda, StringComparison.OrdinalIgnoreCase) ||
                    r.Tour.Contains(model.Busqueda, StringComparison.OrdinalIgnoreCase) ||
                    r.Id.ToString().Contains(model.Busqueda, StringComparison.OrdinalIgnoreCase));
            }
            if (model.Estado != "Todas")
                resultado = resultado.Where(r => r.Estado == model.Estado);

            var ordenadas = resultado.OrderByDescending(r => r.FechaReserva).ThenByDescending(r => r.Id).ToList();
            model.Total = ordenadas.Count;
            model.Pagina = Math.Clamp(pagina, 1, model.TotalPaginas);
            model.Reservas = ordenadas
                .Skip((model.Pagina - 1) * model.TamanoPagina)
                .Take(model.TamanoPagina)
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            model.ApiDisponible = false;
            model.Error = ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? "No tienes permisos para consultar las reservas. Vuelve a iniciar sesión."
                : "No se pudo consultar la API. Comprueba que esté encendida.";
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            model.ApiDisponible = false;
            model.Error = "La API tardó demasiado en responder.";
        }

        return View(model);
    }
}
