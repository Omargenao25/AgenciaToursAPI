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

    [HttpGet("reservas/{reservaId:int}/pasajeros")]
    public async Task<IActionResult> Pasajeros(int reservaId, CancellationToken ct)
    {
        if (reservaId <= 0) return NotFound();
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            var model = await adminApi.ObtenerPasajerosAsync(jwt, reservaId, ct);
            if (model.Estado == "Cancelada" ||
                DateTime.Now >= model.InicioTour.Date.Add(model.Hora))
            {
                TempData["Error"] = "Esta reserva ya no permite modificar pasajeros.";
                return RedirectToAction(nameof(Index));
            }
            Response.Headers.CacheControl = "private, no-store";
            return View(model);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No se pudieron consultar los pasajeros.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("reservas/{reservaId:int}/pasajeros")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pasajeros(int reservaId, EditarPasajerosViewModel model, CancellationToken ct)
    {
        if (reservaId <= 0 || model.Id != reservaId) return BadRequest();
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            var actual = await adminApi.ObtenerPasajerosAsync(jwt, reservaId, ct);
            model.Tour = actual.Tour;
            model.CantidadPersonas = actual.CantidadPersonas;
            model.Estado = actual.Estado;
            model.InicioTour = actual.InicioTour;
            model.Hora = actual.Hora;
            if (model.Pasajeros is null || model.Pasajeros.Count != actual.CantidadPersonas ||
                model.Pasajeros.Any(n => string.IsNullOrWhiteSpace(n) || n.Trim().Length < 3 || n.Trim().Length > 120))
                ModelState.AddModelError(nameof(model.Pasajeros),
                    "Registra un nombre válido por cada plaza reservada.");

            if (ModelState.IsValid)
            {
                var status = await adminApi.ActualizarPasajerosAsync(jwt, model, ct);
                if (status == HttpStatusCode.NoContent)
                {
                    TempData["Exito"] = "Lista de pasajeros actualizada correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", status switch
                {
                    HttpStatusCode.Conflict => "No se puede editar: el tour ya comenzó o la reserva está cancelada.",
                    HttpStatusCode.BadRequest => "Los nombres no coinciden con las plazas reservadas.",
                    _ => $"La API rechazó la actualización (HTTP {(int)status})."
                });
            }
        }
        catch (HttpRequestException) { ModelState.AddModelError("", "La API no está disponible."); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        { ModelState.AddModelError("", "La operación tardó demasiado. Consulta la lista antes de reintentar."); }
        return View(model);
    }

    [HttpPost("{id:int}/manifiesto/check-in/{pasajeroId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(
        int id, int pasajeroId, bool presente, CancellationToken ct)
    {
        if (id <= 0 || pasajeroId <= 0) return NotFound();
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            var status = await adminApi.MarcarAsistenciaAsync(jwt, id, pasajeroId, presente, ct);
            TempData[status == HttpStatusCode.NoContent ? "Exito" : "Error"] = status switch
            {
                HttpStatusCode.NoContent => presente
                    ? "Pasajero marcado como presente." : "Se revirtió el check-in del pasajero.",
                HttpStatusCode.Conflict =>
                    "No se pudo registrar: comprueba que la reserva esté confirmada y el tour aún no haya comenzado.",
                HttpStatusCode.NotFound => "El pasajero no pertenece a este tour.",
                HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized =>
                    "Tu cuenta no tiene permisos para esta acción.",
                _ => $"No se pudo actualizar asistencia (HTTP {(int)status})."
            };
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No fue posible comunicarse con la API. Actualiza el manifiesto antes de reintentar.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado. Comprueba el estado antes de reintentar.";
        }
        return RedirectToAction(nameof(Manifiesto), new { id });
    }

    [HttpGet("{id:int}/cierre-operativo")]
    public async Task<IActionResult> CierreOperativo(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            var manifiesto = await adminApi.ObtenerManifiestoAsync(token, id, ct);
            var estado = await adminApi.ObtenerCierreOperativoAsync(token, id, ct);
            Response.Headers.CacheControl = "private, no-store";
            return View(new CierreOperativoViewModel {
                Manifiesto = manifiesto, Estado = estado
            });
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = ex.StatusCode == HttpStatusCode.NotFound
                ? "El tour solicitado no existe."
                : "No se pudo cargar el cierre operativo. Comprueba la conexión con la API.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La consulta tardó demasiado.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/cierre-operativo")]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CierreOperativo(
        int id, CierreOperativoViewModel model, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");

        if (!model.Confirmar || (model.Incidencias?.Length ?? 0) > 1000)
        {
            TempData["Error"] = "Debes confirmar la revisión del manifiesto y respetar el máximo de 1000 caracteres.";
            return RedirectToAction(nameof(CierreOperativo), new { id });
        }

        try
        {
            var (status, mensaje) = await adminApi.RegistrarCierreOperativoAsync(
                token, id, model.Incidencias, true, ct);
            TempData[status == HttpStatusCode.Created ? "Exito" : "Error"] = status switch
            {
                HttpStatusCode.Created => "Cierre operativo registrado correctamente. La asistencia queda consolidada.",
                HttpStatusCode.Conflict => "No se pudo cerrar: comprueba que no haya solicitudes pendientes, que todos los nombres estén completos, que exista al menos un pasajero presente y que el tour no esté cerrado.",
                HttpStatusCode.BadRequest => "La API rechazó los datos del cierre.",
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                    "No tienes autorización para cerrar una excursión.",
                _ => $"No se pudo completar el cierre (HTTP {(int)status}). {mensaje}"
            };
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No se pudo conectar con la API. Comprueba si la excursión ya fue cerrada antes de repetir.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La operación tardó demasiado. Consulta el estado antes de reintentar.";
        }
        return RedirectToAction(nameof(CierreOperativo), new { id });
    }

    [HttpGet("{id:int}/hoja-salida")]
    public async Task<IActionResult> HojaSalida(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var jwt = Token();
        if (string.IsNullOrWhiteSpace(jwt))
            return RedirectToAction("Acceder", "Cuenta");
        try
        {
            var model = await adminApi.ObtenerManifiestoAsync(jwt, id, ct);
            Response.Headers.CacheControl = "private, no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Referrer-Policy"] = "no-referrer";
            Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
            return View(model);
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = ex.StatusCode switch
            {
                HttpStatusCode.NotFound => "El tour no existe.",
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                    "No tienes autorización para consultar la hoja de salida.",
                _ => "No se pudo cargar la hoja de salida desde la API."
            };
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado. Vuelve a intentarlo.";
        }
        return RedirectToAction(nameof(Index));
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
            if (tour.Estado != "Disponible" || tour.CerradoOperativo)
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
                if (actual.Estado != "Disponible" || actual.CerradoOperativo)
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
