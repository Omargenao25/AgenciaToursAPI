using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace TourSmart.Web.Controllers;

public sealed class TransporteFormulario
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Indica el tipo de transporte.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "El tipo debe tener entre 2 y 50 caracteres.")]
    public string Tipo { get; set; } = "";
    [StringLength(150, ErrorMessage = "La descripción no puede superar los 150 caracteres.")]
    public string Descripcion { get; set; } = "";
}

public sealed class TransportesListado
{
    public List<TransporteFormulario> Transportes { get; set; } = new();
    public TransporteFormulario Transporte { get; set; } = new();
    public HashSet<int> EnUso { get; set; } = new();
}

[Authorize(Roles = "Administrador,Operador")]
[Route("panel/transportes")]
public sealed class TransportesAdminController(IHttpClientFactory factory, IMemoryCache cache) : Controller
{
    private string? Token()
    {
        var id = User.FindFirstValue("SessionId");
        return id is not null && cache.TryGetValue("jwt:" + id, out object? v) ? v as string : null;
    }
    private HttpClient Api() => factory.CreateClient("TourSmart.Sesion");
    private static HttpRequestMessage Request(HttpMethod method, string url, string token)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private sealed class TourReferencia
    {
        public int TransporteId { get; set; }
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? editarId, CancellationToken ct)
    {
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        var model = new TransportesListado();
        try
        {
            using var req = Request(HttpMethod.Get, "api/Transportes", token);
            using var response = await Api().SendAsync(req, ct);
            response.EnsureSuccessStatusCode();
            model.Transportes = await response.Content.ReadFromJsonAsync<List<TransporteFormulario>>(cancellationToken: ct) ?? new();
            model.Transportes = model.Transportes.OrderBy(t => t.Tipo).ThenBy(t => t.Descripcion).ToList();
            using var toursReq = Request(HttpMethod.Get, "api/Tours", token);
            using var toursResp = await Api().SendAsync(toursReq, ct);
            toursResp.EnsureSuccessStatusCode();
            var tours = await toursResp.Content.ReadFromJsonAsync<List<TourReferencia>>(cancellationToken: ct) ?? new();
            model.EnUso = tours.Select(x => x.TransporteId).ToHashSet();
            if (editarId.HasValue)
                model.Transporte = model.Transportes.FirstOrDefault(t => t.Id == editarId.Value) ?? new();
            Response.Headers.CacheControl = "private, no-store";
        }
        catch (HttpRequestException)
        { TempData["Error"] = "No se pudieron cargar los transportes. Comprueba que la API funcione."; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        { TempData["Error"] = "La API tardó demasiado en responder."; }
        return View(model);
    }

    [HttpPost("guardar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar([Bind(Prefix = "Transporte")] TransporteFormulario model, CancellationToken ct)
    {
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");
        model.Tipo = model.Tipo?.Trim() ?? "";
        model.Descripcion = model.Descripcion?.Trim() ?? "";
        if (model.Id < 0 || !TryValidateModel(model))
        {
            TempData["Error"] = "Revisa el tipo y la descripción (máximo 50 y 150 caracteres).";
            return RedirectToAction(nameof(Index), model.Id > 0 ? new { editarId = model.Id } : null);
        }
        try
        {
            using var req = Request(model.Id == 0 ? HttpMethod.Post : HttpMethod.Put,
                model.Id == 0 ? "api/Transportes" : $"api/Transportes/{model.Id}", token);
            req.Content = JsonContent.Create(new { model.Id, model.Tipo, model.Descripcion });
            using var resp = await Api().SendAsync(req, ct);
            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.StatusCode switch
            {
                HttpStatusCode.Created => "Transporte registrado. Ya puedes asignarlo a un tour.",
                HttpStatusCode.NoContent => "Transporte actualizado correctamente.",
                HttpStatusCode.BadRequest or HttpStatusCode.Conflict => "El transporte ya existe o los datos son inválidos.",
                HttpStatusCode.NotFound => "El transporte no existe.",
                HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "Tu cuenta no tiene permisos.",
                _ => $"No fue posible guardar (HTTP {(int)resp.StatusCode})."
            };
        }
        catch (HttpRequestException)
        { TempData["Error"] = "Sin conexión con la API. Verifica el listado antes de volver a guardar."; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        { TempData["Error"] = "La operación tardó demasiado. Comprueba si el transporte fue guardado."; }
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
            using var req = Request(HttpMethod.Delete, $"api/Transportes/{id}", token);
            using var resp = await Api().SendAsync(req, ct);
            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.StatusCode switch
            {
                HttpStatusCode.NoContent => "Transporte eliminado correctamente.",
                HttpStatusCode.BadRequest or HttpStatusCode.Conflict => "No se puede eliminar porque está asignado a uno o más tours.",
                HttpStatusCode.NotFound => "El transporte ya no existe.",
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
