using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace TourSmart.Web.Controllers;

public sealed class GuiaFormulario
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Indica el nombre de guía.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "Indica el teléfono del guía.")]
    [StringLength(20, MinimumLength = 7, ErrorMessage = "El teléfono debe tener entre 7 y 20 caracteres.")]
    public string Telefono { get; set; } = "";
}

public sealed class GuiasListado
{
    public List<GuiaFormulario> Guias { get; set; } = new();
    public GuiaFormulario Guia { get; set; } = new();
    public HashSet<int> EnUso { get; set; } = new();
}

[Authorize(Roles = "Administrador,Operador")]
[Route("panel/guías")]
public sealed class GuiasAdminController(IHttpClientFactory factory, IMemoryCache cache) : Controller
{
    private string? Token()
    {
        var id = User.FindFirstValue("SessionId");
        return id is not null && cache.TryGetValue("jwt:" + id, out object? v) ? v as string : null;
    }
    private HttpClient Api() => factory.CreateClient("TourSmart.Sesion");
    private static HttpRequestMessage CrearPeticion(HttpMethod method, string url, string token)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private sealed class TourReferencia
    {
        public int GuiaId { get; set; }
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? editarId, CancellationToken ct)
    {
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        var model = new GuiasListado();
        try
        {
            using var req = CrearPeticion(HttpMethod.Get, "api/Guias", token);
            using var response = await Api().SendAsync(req, ct);
            response.EnsureSuccessStatusCode();
            model.Guias = await response.Content.ReadFromJsonAsync<List<GuiaFormulario>>(cancellationToken: ct) ?? new();
            model.Guias = model.Guias.OrderBy(t => t.Nombre).ThenBy(t => t.Telefono).ToList();
            using var toursReq = CrearPeticion(HttpMethod.Get, "api/Tours", token);
            using var toursResp = await Api().SendAsync(toursReq, ct);
            toursResp.EnsureSuccessStatusCode();
            var tours = await toursResp.Content.ReadFromJsonAsync<List<TourReferencia>>(cancellationToken: ct) ?? new();
            model.EnUso = tours.Select(x => x.GuiaId).ToHashSet();
            if (editarId.HasValue)
                model.Guia = model.Guias.FirstOrDefault(t => t.Id == editarId.Value) ?? new();
            Response.Headers.CacheControl = "private, no-store";
        }
        catch (HttpRequestException)
        { TempData["Error"] = "No se pudieron cargar los guías. Comprueba que la API funcione."; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        { TempData["Error"] = "La API tardó demasiado en responder."; }
        return View(model);
    }

    [HttpPost("guardar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar([Bind(Prefix = "Guia")] GuiaFormulario model, CancellationToken ct)
    {
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        model.Nombre = model.Nombre?.Trim() ?? "";
        model.Telefono = model.Telefono?.Trim() ?? "";
        if (model.Id < 0 || !TryValidateModel(model))
        {
            TempData["Error"] = "Revisa el nombre (2-100 caracteres) y el teléfono (7-20 caracteres).";
            return RedirectToAction(nameof(Index), model.Id > 0 ? new { editarId = model.Id } : null);
        }
        try
        {
            using var req = CrearPeticion(model.Id == 0 ? HttpMethod.Post : HttpMethod.Put,
                model.Id == 0 ? "api/Guias" : $"api/Guias/{model.Id}", token);
            req.Content = JsonContent.Create(new { model.Id, model.Nombre, model.Telefono });
            using var resp = await Api().SendAsync(req, ct);
            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.StatusCode switch
            {
                HttpStatusCode.Created => "Guía registrado. Ya puedes asignarlo a un tour.",
                HttpStatusCode.NoContent => "Guía actualizado correctamente.",
                HttpStatusCode.BadRequest or HttpStatusCode.Conflict => "El guía ya existe o los datos son inválidos.",
                HttpStatusCode.NotFound => "El guía no existe.",
                HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "Tu cuenta no tiene permisos.",
                _ => $"No fue posible guardar (HTTP {(int)resp.StatusCode})."
            };
        }
        catch (HttpRequestException)
        { TempData["Error"] = "Sin conexión con la API. Verifica el listado antes de volver a guardar."; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        { TempData["Error"] = "La operación tardó demasiado. Comprueba si el guía fue guardado."; }
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
            using var req = CrearPeticion(HttpMethod.Delete, $"api/Guias/{id}", token);
            using var resp = await Api().SendAsync(req, ct);
            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.StatusCode switch
            {
                HttpStatusCode.NoContent => "Guía eliminado correctamente.",
                HttpStatusCode.BadRequest or HttpStatusCode.Conflict => "No se puede eliminar porque está asignado a uno o más tours.",
                HttpStatusCode.NotFound => "El guía ya no existe.",
                HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "No tienes permisos.",
                _ => $"No se pudo eliminar (HTTP {(int)resp.StatusCode})."
            };
        }
        catch (HttpRequestException)
        { TempData["Error"] = "Sin conexión con la API. Comprueba el listado antes de volver a eliminar."; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        { TempData["Error"] = "La operación tardó demasiado. Comprueba el listado."; }
        return RedirectToAction(nameof(Index));
    }
}
