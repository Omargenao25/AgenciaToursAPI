using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TourSmart.Web.Models;
using TourSmart.Web.Services;

namespace TourSmart.Web.Controllers;

[Authorize(Roles = "Administrador,Operador")]
[Route("panel/clientes")]
public sealed class ClientesAdminController(IClientesAdminClient clientesApi, IMemoryCache cache) : Controller
{
    private string? Token()
    {
        var id = User.FindFirstValue("SessionId");
        return id is not null && cache.TryGetValue("jwt:" + id, out object? value)
            ? value as string : null;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? busqueda, int pagina = 1, CancellationToken ct = default)
    {
        var model = new ClientesListadoViewModel
        {
            Busqueda = (busqueda ?? "").Trim().Length > 100 ? (busqueda ?? "").Trim()[..100] : (busqueda ?? "").Trim()
        };
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");

        try
        {
            var clientes = await clientesApi.ListarAsync(token, ct);
            var filtrados = clientes.Where(c =>
                string.IsNullOrWhiteSpace(model.Busqueda) ||
                c.Nombre.Contains(model.Busqueda, StringComparison.OrdinalIgnoreCase) ||
                c.Apellido.Contains(model.Busqueda, StringComparison.OrdinalIgnoreCase) ||
                (c.Nombre + " " + c.Apellido).Contains(model.Busqueda, StringComparison.OrdinalIgnoreCase) ||
                (c.NumeroDocumento ?? "").Contains(model.Busqueda, StringComparison.OrdinalIgnoreCase) ||
                c.TipoDocumento.Contains(model.Busqueda, StringComparison.OrdinalIgnoreCase) ||
                c.Correo.Contains(model.Busqueda, StringComparison.OrdinalIgnoreCase) ||
                c.Id.ToString().Contains(model.Busqueda, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Apellido).ThenBy(c => c.Nombre).ToList();

            model.Total = filtrados.Count;
            model.Pagina = Math.Clamp(pagina, 1, model.TotalPaginas);
            model.Clientes = filtrados.Skip((model.Pagina - 1) * model.TamanoPagina)
                .Take(model.TamanoPagina).ToList();
        }
        catch (HttpRequestException ex)
        {
            model.ApiDisponible = false;
            model.Error = ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized
                ? "La API rechazó los permisos. Inicia sesión nuevamente."
                : "No se pudo cargar el listado de clientes. Comprueba la API.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            model.ApiDisponible = false;
            model.Error = "La API tardó demasiado en responder.";
        }
        return View(model);
    }

    [HttpGet("crear")]
    public IActionResult Crear() => View("Formulario", new ClienteFormularioViewModel());

    [HttpGet("{id:int}/editar")]
    public async Task<IActionResult> Editar(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");

        try
        {
            var cliente = await clientesApi.ObtenerAsync(token, id, ct);
            return cliente is null ? NotFound() : View("Formulario", cliente);
        }
        catch (HttpRequestException) { TempData["Error"] = "No se pudo cargar el cliente desde la API."; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { TempData["Error"] = "La API tardó demasiado en responder."; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guardar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(ClienteFormularioViewModel model, CancellationToken ct)
    {
        if (model.Id < 0) return BadRequest();
        var token = Token();
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Acceder", "Cuenta");

        model.Nombre = model.Nombre?.Trim() ?? "";
        model.Apellido = model.Apellido?.Trim() ?? "";
        model.TipoDocumento = model.TipoDocumento?.Trim() ?? "";
        model.NumeroDocumento = model.NumeroDocumento?.Trim().ToUpperInvariant() ?? "";
        model.PaisEmisorDocumento = model.PaisEmisorDocumento?.Trim() ?? "";
        model.Cedula = model.TipoDocumento == "CedulaDominicana" ? model.NumeroDocumento : null;
        model.Telefono = model.Telefono?.Trim() ?? "";
        model.Correo = model.Correo?.Trim().ToLowerInvariant() ?? "";

        if (model.Id > 0 && ModelState.IsValid)
        {
            try
            {
                if (await clientesApi.ObtenerAsync(token, model.Id, ct) is null)
                    return NotFound();
            }
            catch (HttpRequestException) { ModelState.AddModelError("", "No se pudo verificar si el cliente existe."); }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested) { ModelState.AddModelError("", "La API no respondió al verificar el cliente."); }
        }

        if (ModelState.IsValid)
        {
            try
            {
                var (status, message) = await clientesApi.GuardarAsync(token, model, ct);
                if (status is HttpStatusCode.Created or HttpStatusCode.NoContent)
                {
                    TempData["Exito"] = model.Id == 0
                        ? "¡Cliente registrado correctamente!"
                        : "¡Datos del cliente actualizados!";
                    return RedirectToAction(nameof(Index));
                }

                var yaExiste = status == HttpStatusCode.BadRequest &&
                    !string.IsNullOrEmpty(message) &&
                    message.Contains("Ya existe", StringComparison.OrdinalIgnoreCase);

                ModelState.AddModelError("", status switch
                {
                    HttpStatusCode.BadRequest when yaExiste => "Ya existe otro cliente con ese documento o correo. Comprueba los datos.",
                    HttpStatusCode.BadRequest => "La API rechazó los datos del cliente. Revisa los campos.",
                    HttpStatusCode.NotFound => "El cliente ya no existe.",
                    HttpStatusCode.Conflict => "Se detectó un conflicto al guardar. Revisa los datos.",
                    HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "La API no autorizó esta operación.",
                    _ => $"No se pudo guardar el cliente (HTTP {(int)status})."
                });
            }
            catch (HttpRequestException) { ModelState.AddModelError("", "No se pudo conectar con la API."); }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested) { ModelState.AddModelError("", "La API tardó demasiado; revisa el listado antes de repetir para evitar duplicados."); }
        }
        return View("Formulario", model);
    }
}
