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
[Route("panel/reservas")]
public sealed class ReservasWebController(IReservasAdminClient reservasApi, IToursApiClient toursApi, IMemoryCache cache) : Controller
{
    private string? ObtenerToken()
    {
        var sessionId = User.FindFirstValue("SessionId");
        return sessionId is not null && cache.TryGetValue("jwt:" + sessionId, out object? value)
            ? value as string : null;
    }

    private async Task CargarOpciones(CrearReservaViewModel model, string token, CancellationToken ct)
    {
        var catalogos = await reservasApi.CatalogosAsync(token, ct);
        var tours = await toursApi.GetToursAsync(ct);
        model.Clientes = catalogos.Clientes
            .OrderBy(x => x.Nombre)
            .Select(x => new SelectListItem($"{x.Nombre} {x.Apellido}", x.Id.ToString()))
            .ToList();
        model.MetodosPago = catalogos.Metodos
            .Select(x => new SelectListItem(x.Nombre, x.Id.ToString()))
            .ToList();
        model.Tours = tours
            .Where(x => x.Estado == "Disponible" && x.Fecha.Date.Add(x.Hora) > DateTime.Now && x.CuposDisponibles > 0)
            .OrderBy(x => x.Fecha)
            .Select(x => new SelectListItem(
                $"{x.Nombre} · {x.Fecha:dd/MM/yyyy} · {x.CuposDisponibles} plazas · RD$ {x.Precio:N2}",
                x.Id.ToString()))
            .ToList();
    }

    [HttpGet("crear")]
    public async Task<IActionResult> Crear(int? tourId, CancellationToken cancellationToken)
    {
        var token = ObtenerToken();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        var model = new CrearReservaViewModel { TourId = tourId.GetValueOrDefault() };
        try
        {
            await CargarOpciones(model, token, cancellationToken);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No se pudieron cargar los clientes, métodos de pago o tours.";
            return RedirectToAction(nameof(Index));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado en cargar el formulario.";
            return RedirectToAction(nameof(Index));
        }
        return View("Crear", model);
    }

    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearReservaViewModel model, CancellationToken cancellationToken)
    {
        var token = ObtenerToken();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");

        try
        {
            await CargarOpciones(model, token, cancellationToken);
            if (ModelState.IsValid)
            {
                if (!model.Clientes.Any(x => x.Value == model.ClienteId.ToString()))
                    ModelState.AddModelError(nameof(model.ClienteId), "El cliente seleccionado no existe.");
                if (!model.MetodosPago.Any(x => x.Value == model.MetodoPagoId.ToString()))
                    ModelState.AddModelError(nameof(model.MetodoPagoId), "El método de pago seleccionado no existe.");
                if (!model.Tours.Any(x => x.Value == model.TourId.ToString()))
                    ModelState.AddModelError(nameof(model.TourId), "El tour no está disponible o no tiene plazas.");
            }

            if (ModelState.IsValid)
            {
                var resultado = await reservasApi.CrearAsync(token, model, cancellationToken);
                if (resultado.Estado == HttpStatusCode.Created)
                {
                    TempData["Exito"] = "Reserva creada correctamente en estado Pendiente.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", resultado.Estado switch
                {
                    HttpStatusCode.Conflict => "No hay suficientes plazas. Actualiza el tour e inténtalo de nuevo.",
                    HttpStatusCode.BadRequest => "La API rechazó la reserva. Comprueba los datos y la fecha del tour.",
                    HttpStatusCode.NotFound => "El cliente, tour o método de pago ya no existe.",
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Tu sesión no tiene permisos para crear reservas.",
                    _ => $"No se pudo registrar la reserva (HTTP {(int)resultado.Estado})."
                });
            }
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "No se pudo contactar con la API. Comprueba que esté encendida.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            ModelState.AddModelError("", "La operación tardó demasiado. Consulta el listado antes de repetirla para evitar duplicados.");
        }

        return View("Crear", model);
    }

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
