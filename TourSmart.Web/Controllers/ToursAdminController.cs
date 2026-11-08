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
[Route("panel/tours")]
public sealed class ToursAdminController(IToursApiClient toursApi, IToursAdminClient adminApi, IMemoryCache cache) : Controller
{
    private string? Token()
    {
        var id = User.FindFirstValue("SessionId");
        return id is not null && cache.TryGetValue("jwt:" + id, out object? value) ? value as string : null;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try { return View(await toursApi.GetToursAsync(ct)); }
        catch (HttpRequestException) { TempData["Error"] = "No se pudo consultar la API de tours."; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { TempData["Error"] = "La API tardó demasiado en responder."; }
        return View(Array.Empty<TourCard>());
    }

    private async Task CargarCatalogos(TourFormularioViewModel model, string token, CancellationToken ct)
    {
        var catalogos = await adminApi.CatalogosAsync(token, ct);
        model.Paises = catalogos.Paises.Select(x => new SelectListItem(x.Nombre, x.Id.ToString())).ToList();
        model.Destinos = catalogos.Destinos.Select(x => new SelectListItem(x.Nombre, x.Id.ToString())).ToList();
        model.Categorias = catalogos.Categorias.Select(x => new SelectListItem(x.Nombre, x.Id.ToString())).ToList();
        model.Guias = catalogos.Guias.Select(x => new SelectListItem(x.Nombre, x.Id.ToString())).ToList();
        model.Transportes = catalogos.Transportes.Select(x => new SelectListItem(x.Tipo, x.Id.ToString())).ToList();
        ViewBag.DestinosPorPais = System.Text.Json.JsonSerializer.Serialize(catalogos.Destinos.Select(x => new { x.Id, x.PaisId }));
    }

    [HttpGet("{id:int}/manifiesto")]
    public async Task<IActionResult> Manifiesto(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            var model = await adminApi.ObtenerManifiestoAsync(token, id, ct);
            Response.Headers.CacheControl = "private, no-store";
            return View(model);
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = ex.StatusCode switch
            {
                HttpStatusCode.NotFound => "No existe el tour solicitado.",
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                    "No tienes permiso para consultar el manifiesto.",
                _ => "No fue posible consultar las reservas del tour."
            };
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado en responder.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/adelanto")]
    public async Task<IActionResult> Adelanto(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            return View(await adminApi.ObtenerAdelantoAsync(jwt, id, ct));
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No se pudo consultar la política de adelanto del tour.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/adelanto")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adelanto(int id, AdelantoTourViewModel model, CancellationToken ct)
    {
        if (id <= 0 || model.Id != id) return BadRequest();
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            var actual = await adminApi.ObtenerAdelantoAsync(jwt, id, ct);
            model.Nombre = actual.Nombre;
            model.TieneReservas = actual.TieneReservas;
            if (actual.TieneReservas) model.HorasLimiteAdelanto = actual.HorasLimiteAdelanto;
            if (actual.TieneReservas)
                ModelState.AddModelError("", "Este tour ya tiene reservas. No se puede modificar el adelanto acordado.");

            if (ModelState.IsValid)
            {
                var status = await adminApi.ConfigurarAdelantoAsync(jwt, model, ct);
                if (status == HttpStatusCode.NoContent)
                {
                    TempData["Exito"] = $"Adelanto de {model.PorcentajeAdelanto}% y plazo de {model.HorasLimiteAdelanto} horas configurados correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", status switch
                {
                    HttpStatusCode.Conflict => "No se pudo actualizar: el tour ya comenzó o tiene reservas registradas.",
                    HttpStatusCode.BadRequest => "El porcentaje debe estar entre 0 y 100.",
                    _ => $"No se pudo actualizar la política de adelanto (HTTP {(int)status})."
                });
            }
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "No fue posible comunicarse con la API.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            ModelState.AddModelError("", "La solicitud tardó demasiado. Verifica el valor antes de reintentar.");
        }
        return View(model);
    }

    [HttpGet("crear")]
    public async Task<IActionResult> Crear(CancellationToken ct)
    {
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        var model = new TourFormularioViewModel();
        try { await CargarCatalogos(model, token, ct); }
        catch (HttpRequestException) { TempData["Error"] = "No se pudieron cargar los catálogos."; return RedirectToAction(nameof(Index)); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { TempData["Error"] = "La API no respondió."; return RedirectToAction(nameof(Index)); }
        return View("Formulario", model);
    }

    [HttpGet("{id:int}/editar")]
    public async Task<IActionResult> Editar(int id, CancellationToken ct)
    {
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            var tour = await toursApi.GetTourAsync(id, ct);
            if (tour is null) return NotFound();
            if (tour.Estado != "Disponible")
            {
                TempData["Error"] = "Solo se pueden editar tours que todavía no han comenzado.";
                return RedirectToAction(nameof(Index));
            }
            var model = new TourFormularioViewModel
            {
                Id = tour.Id, Nombre = tour.Nombre, PaisId = tour.PaisId,
                DestinoId = tour.DestinoId, CategoriaId = tour.CategoriaId,
                GuiaId = tour.GuiaId, TransporteId = tour.TransporteId,
                Fecha = tour.Fecha, Hora = tour.Hora, Precio = tour.Precio,
                CapacidadMaxima = tour.CapacidadMaxima, CuposReservados = tour.CuposReservados
            };
            await CargarCatalogos(model, token, ct);
            return View("Formulario", model);
        }
        catch (HttpRequestException) { TempData["Error"] = "No se pudo consultar la API."; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { TempData["Error"] = "La API no respondió."; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guardar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(TourFormularioViewModel model, CancellationToken ct)
    {
        if (model.Id < 0) return BadRequest();
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");

        try
        {
            var actual = model.Id > 0 ? await toursApi.GetTourAsync(model.Id, ct) : null;
            if (model.Id > 0 && actual is null) return NotFound();
            if (actual is not null)
            {
                model.CuposReservados = actual.CuposReservados;
                if (actual.Estado != "Disponible")
                    ModelState.AddModelError("", "No se puede editar un tour que ya comenzó.");
                if (model.CapacidadMaxima < actual.CuposReservados)
                    ModelState.AddModelError(nameof(model.CapacidadMaxima), "La capacidad es inferior a los cupos ocupados.");
                if (actual.CuposReservados > 0 &&
                    (model.PaisId != actual.PaisId || model.DestinoId != actual.DestinoId ||
                     model.GuiaId != actual.GuiaId || model.TransporteId != actual.TransporteId ||
                     model.Fecha.Date != actual.Fecha.Date || model.Hora != actual.Hora))
                    ModelState.AddModelError("", "No puedes cambiar fecha, hora, país, destino, guía ni transporte mientras existan reservas activas.");
            }

            if (model.Fecha.Date.Add(model.Hora) <= DateTime.Now)
                ModelState.AddModelError(nameof(model.Fecha), "La fecha y hora deben ser futuras.");

            if (ModelState.IsValid)
            {
                var status = await adminApi.GuardarAsync(token, model, ct);
                if (status is HttpStatusCode.Created or HttpStatusCode.NoContent)
                {
                    TempData["Exito"] = model.Id == 0 ? "¡Tour creado correctamente!" : "¡Tour actualizado correctamente!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", status switch
                {
                    HttpStatusCode.Conflict => "Los cupos disponibles cambiaron. Revisa la capacidad del tour.",
                    HttpStatusCode.BadRequest => "La API rechazó los datos. Comprueba fechas, relaciones y restricciones de reservas.",
                    HttpStatusCode.NotFound => "Alguno de los catálogos o el tour ya no existe.",
                    HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "La API no autorizó la operación.",
                    _ => "No se pudo guardar el tour. La API devolvió: " + (int)status
                });
            }
            await CargarCatalogos(model, token, ct);
        }
        catch (HttpRequestException) { ModelState.AddModelError("", "No se pudo conectar con la API."); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { ModelState.AddModelError("", "La API tardó demasiado en responder; revisa el listado antes de volver a guardar."); }

        if (model.Paises.Count == 0)
        {
            try { await CargarCatalogos(model, token, ct); }
            catch (HttpRequestException) { ModelState.AddModelError("", "No se pudieron cargar los catálogos."); }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested) { ModelState.AddModelError("", "No se pudieron cargar los catálogos."); }
        }
        return View("Formulario", model);
    }
}
