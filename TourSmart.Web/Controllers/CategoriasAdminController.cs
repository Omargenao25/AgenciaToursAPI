using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace TourSmart.Web.Controllers;

public sealed class CategoriaFormulario
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Escribe un nombre de categoria.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}

public sealed class CategoriasListado
{
    public List<CategoriaFormulario> Categorias { get; set; } = new();
    public CategoriaFormulario Categoria { get; set; } = new();
    public HashSet<int> CategoriasEnUso { get; set; } = new();
}

[Authorize(Roles = "Administrador,Operador")]
[Route("panel/categorias")]
public sealed class CategoriasAdminController(IHttpClientFactory httpClientFactory, IMemoryCache cache) : Controller
{
    private HttpClient Api() => httpClientFactory.CreateClient("TourSmart.Sesion");

    private string? Token()
    {
        var sessionId = User.FindFirstValue("SessionId");
        return sessionId is not null && cache.TryGetValue("jwt:" + sessionId, out object? token)
            ? token as string : null;
    }

    private static HttpRequestMessage CrearPeticion(HttpMethod method, string path, string token)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? editarId, CancellationToken ct)
    {
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        var model = new CategoriasListado();

        try
        {
            using var request = CrearPeticion(HttpMethod.Get, "api/Categorias", token);
            using var response = await Api().SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            model.Categorias = await response.Content.ReadFromJsonAsync<List<CategoriaFormulario>>(cancellationToken: ct)
                ?? new List<CategoriaFormulario>();
            model.Categorias = model.Categorias.OrderBy(c => c.Nombre).ToList();

            // Los tours existentes determinan si la API permitira eliminar una categoria.
            using var toursResponse = await Api().GetAsync("api/Tours", ct);
            toursResponse.EnsureSuccessStatusCode();
            var tours = await toursResponse.Content.ReadFromJsonAsync<List<TourCategoriaReferencia>>(cancellationToken: ct)
                ?? new List<TourCategoriaReferencia>();
            model.CategoriasEnUso = tours.Select(t => t.CategoriaId).ToHashSet();

            if (editarId.HasValue)
                model.Categoria = model.Categorias.FirstOrDefault(c => c.Id == editarId.Value)
                    ?? new CategoriaFormulario();

            Response.Headers.CacheControl = "private, no-store";
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No fue posible cargar las categorias desde la API.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardo demasiado. Intenta de nuevo.";
        }
        return View(model);
    }

    private sealed class TourCategoriaReferencia
    {
        public int CategoriaId { get; set; }
    }

    [HttpPost("guardar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar([Bind(Prefix = "Categoria")] CategoriaFormulario model, CancellationToken ct)
    {
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        model.Nombre = model.Nombre?.Trim() ?? "";

        if (model.Id < 0 || !TryValidateModel(model))
        {
            TempData["Error"] = "Introduce un nombre valido de 2 a 50 caracteres.";
            return RedirectToAction(nameof(Index), model.Id > 0 ? new { editarId = model.Id } : null);
        }

        try
        {
            var ruta = model.Id == 0 ? "api/Categorias" : $"api/Categorias/{model.Id}";
            using var request = CrearPeticion(model.Id == 0 ? HttpMethod.Post : HttpMethod.Put, ruta, token);
            request.Content = JsonContent.Create(new { model.Id, model.Nombre });
            using var response = await Api().SendAsync(request, ct);
            TempData[response.IsSuccessStatusCode ? "Exito" : "Error"] = response.StatusCode switch
            {
                HttpStatusCode.Created => "Categoria creada correctamente. Ya puedes asignarla a un tour.",
                HttpStatusCode.NoContent => "Categoria actualizada correctamente.",
                HttpStatusCode.BadRequest or HttpStatusCode.Conflict => "Nombre invalido o duplicado. Revisa la lista e intenta nuevamente.",
                HttpStatusCode.NotFound => "La categoria ya no existe.",
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "No tienes permisos para editar categorias.",
                _ => $"No se pudo guardar la categoria (HTTP {(int)response.StatusCode})."
            };
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No se pudo contactar a la API. Comprueba la lista antes de reintentar.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La operacion tardo demasiado. Verifica si se guardo antes de reintentar.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");

        try
        {
            using var request = CrearPeticion(HttpMethod.Delete, $"api/Categorias/{id}", token);
            using var response = await Api().SendAsync(request, ct);
            TempData[response.IsSuccessStatusCode ? "Exito" : "Error"] = response.StatusCode switch
            {
                HttpStatusCode.NoContent => "Categoria eliminada.",
                HttpStatusCode.BadRequest or HttpStatusCode.Conflict => "No puedes eliminar una categoria que tenga tours asociados.",
                HttpStatusCode.NotFound => "La categoria ya no existe.",
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "No tienes permisos para eliminar categorias.",
                _ => $"No se pudo eliminar (HTTP {(int)response.StatusCode})."
            };
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "La API no responde. Consulta el listado antes de intentar eliminar nuevamente.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La operacion tardo demasiado. Verifica el estado antes de repetir.";
        }
        return RedirectToAction(nameof(Index));
    }
}
