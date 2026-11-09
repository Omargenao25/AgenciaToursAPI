using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace TourSmart.Web.Controllers;

public sealed class PaisFormulario
{
    public int Id { get; set; }
    [Required, StringLength(100, MinimumLength = 2)]
    public string Nombre { get; set; } = "";
}

public sealed class DestinoFormulario
{
    public int Id { get; set; }
    [Required, StringLength(100, MinimumLength = 2)]
    public string Nombre { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un país.")]
    public int PaisId { get; set; }
    [Range(0, 365)]
    public int DuracionDias { get; set; }
    [Range(0, 23)]
    public int DuracionHoras { get; set; }
}

public sealed class GeografiaListado
{
    public List<PaisFormulario> Paises { get; set; } = new();
    public List<DestinoFormulario> Destinos { get; set; } = new();
    public PaisFormulario Pais { get; set; } = new();
    public DestinoFormulario Destino { get; set; } = new();
}

[Authorize(Roles = "Administrador,Operador")]
[Route("panel/geografia")]
public sealed class GeografiaAdminController(IHttpClientFactory httpClientFactory, IMemoryCache cache) : Controller
{
    private string? Token()
    {
        var sessionId = User.FindFirstValue("SessionId");
        return sessionId is not null && cache.TryGetValue("jwt:" + sessionId, out object? value)
            ? value as string : null;
    }

    private HttpRequestMessage CrearPeticion(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private HttpClient Api() => httpClientFactory.CreateClient("TourSmart.Sesion");

    [HttpGet("")]
    public async Task<IActionResult> Index(int? paisId, int? destinoId, CancellationToken ct)
    {
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");

        var model = new GeografiaListado();
        try
        {
            using var paisesRequest = CrearPeticion(HttpMethod.Get, "api/Paises", token);
            using var destinosRequest = CrearPeticion(HttpMethod.Get, "api/Destinos", token);
            using var paisesResponse = await Api().SendAsync(paisesRequest, ct);
            using var destinosResponse = await Api().SendAsync(destinosRequest, ct);
            paisesResponse.EnsureSuccessStatusCode();
            destinosResponse.EnsureSuccessStatusCode();
            model.Paises = await paisesResponse.Content.ReadFromJsonAsync<List<PaisFormulario>>(cancellationToken: ct) ?? new();
            model.Destinos = await destinosResponse.Content.ReadFromJsonAsync<List<DestinoFormulario>>(cancellationToken: ct) ?? new();
            model.Paises = model.Paises.OrderBy(p => p.Nombre).ToList();
            model.Destinos = model.Destinos.OrderBy(d => d.Nombre).ToList();
            if (paisId.HasValue)
                model.Pais = model.Paises.FirstOrDefault(p => p.Id == paisId.Value) ?? new();
            if (destinoId.HasValue)
                model.Destino = model.Destinos.FirstOrDefault(d => d.Id == destinoId.Value) ?? new();
            Response.Headers.CacheControl = "private, no-store";
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No fue posible obtener países y destinos desde la API.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado en responder.";
        }
        return View(model);
    }

    [HttpPost("pais")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarPais([Bind(Prefix = "Pais")] PaisFormulario model, CancellationToken ct)
    {
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        model.Nombre = model.Nombre?.Trim() ?? "";
        if (!TryValidateModel(model) || model.Id < 0)
        {
            TempData["Error"] = "Escribe un nombre de país válido (2 a 100 caracteres).";
            return RedirectToAction(nameof(Index), new { paisId = model.Id > 0 ? model.Id : (int?)null });
        }
        return await Guardar("api/Paises", model.Id, model, token, ct, "País");
    }

    [HttpPost("destino")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarDestino([Bind(Prefix = "Destino")] DestinoFormulario model, CancellationToken ct)
    {
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        model.Nombre = model.Nombre?.Trim() ?? "";
        if (!TryValidateModel(model) || model.Id < 0 || (model.DuracionDias == 0 && model.DuracionHoras == 0))
        {
            TempData["Error"] = "Indica nombre, país y una duración mayor que cero.";
            return RedirectToAction(nameof(Index), new { destinoId = model.Id > 0 ? model.Id : (int?)null });
        }
        return await Guardar("api/Destinos", model.Id, model, token, ct, "Destino");
    }

    private async Task<IActionResult> Guardar(string ruta, int id, object datos, string token, CancellationToken ct, string tipo)
    {
        try
        {
            using var request = CrearPeticion(id == 0 ? HttpMethod.Post : HttpMethod.Put,
                id == 0 ? ruta : $"{ruta}/{id}", token);
            request.Content = JsonContent.Create(datos);
            using var response = await Api().SendAsync(request, ct);
            TempData[response.IsSuccessStatusCode ? "Exito" : "Error"] = response.StatusCode switch
            {
                HttpStatusCode.Created => $"{tipo} creado correctamente.",
                HttpStatusCode.NoContent => $"{tipo} actualizado correctamente.",
                HttpStatusCode.BadRequest => "Datos inválidos, duplicados o cambios incompatibles con tours existentes.",
                HttpStatusCode.NotFound => "El registro o país seleccionado no existe.",
                HttpStatusCode.Conflict => "No se puede realizar esta operación mientras existen relaciones asociadas.",
                HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "No tienes permisos para actualizar el catálogo.",
                _ => $"No se pudo guardar el registro (HTTP {(int)response.StatusCode})."
            };
        }
        catch (HttpRequestException) { TempData["Error"] = "No se pudo conectar con la API. Revisa el catálogo antes de reintentar."; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        { TempData["Error"] = "La API tardó demasiado. Verifica si se guardó antes de reintentar."; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("pais/{id:int}/eliminar")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> EliminarPais(int id, CancellationToken ct)
        => Eliminar("api/Paises", id, "País", ct);

    [HttpPost("destino/{id:int}/eliminar")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> EliminarDestino(int id, CancellationToken ct)
        => Eliminar("api/Destinos", id, "Destino", ct);

    private async Task<IActionResult> Eliminar(string ruta, int id, string tipo, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            using var request = CrearPeticion(HttpMethod.Delete, $"{ruta}/{id}", token);
            using var response = await Api().SendAsync(request, ct);
            TempData[response.IsSuccessStatusCode ? "Exito" : "Error"] = response.StatusCode switch
            {
                HttpStatusCode.NoContent => $"{tipo} eliminado.",
                HttpStatusCode.BadRequest or HttpStatusCode.Conflict =>
                    "No se puede eliminar: el registro está asociado a destinos o tours.",
                HttpStatusCode.NotFound => "El registro ya no existe.",
                HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "No tienes permiso para eliminar.",
                _ => $"No se pudo eliminar (HTTP {(int)response.StatusCode})."
            };
        }
        catch (HttpRequestException) { TempData["Error"] = "No se pudo conectar a la API. Comprueba si el registro sigue ahí."; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        { TempData["Error"] = "La operación tardó demasiado. Comprueba el catálogo antes de reintentar."; }
        return RedirectToAction(nameof(Index));
    }
}
