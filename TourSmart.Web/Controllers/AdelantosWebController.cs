using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TourSmart.Web.Models;

namespace TourSmart.Web.Controllers;

[Authorize]
[Route("adelantos")]
public sealed class AdelantosWebController(IHttpClientFactory factory, IMemoryCache cache, IConfiguration configuration) : Controller
{
    private const string Base = "api/comprobantes-transferencia";
    private HttpClient Api() => factory.CreateClient("TourSmart.Comprobantes");
    private string? Jwt()
    {
        var session = User.FindFirstValue("SessionId");
        return session is not null && cache.TryGetValue("jwt:" + session, out object? value)
            ? value as string : null;
    }
    private static HttpRequestMessage Req(HttpMethod method, string route, string jwt)
    {
        var r = new HttpRequestMessage(method, route);
        r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return r;
    }
    private async Task<(HttpStatusCode Estado, T? Valor)> LeerAsync<T>(string route, string jwt, CancellationToken ct)
    {
        using var request = Req(HttpMethod.Get, route, jwt);
        using var response = await Api().SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return (response.StatusCode, default);
        return (response.StatusCode, await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct));
    }

    // El resumen financiero se consulta desde la API usando exclusivamente el JWT del cliente.
    private sealed class EstadoPagoCliente
    {
        public int ReservaId { get; set; }
        public string Tour { get; set; } = "";
        public string EstadoReserva { get; set; } = "";
        public decimal Total { get; set; }
        public decimal Pagado { get; set; }
        public decimal Devuelto { get; set; }
        public int PorcentajeAdelanto { get; set; }
        public DateTime? VencePendienteUtc { get; set; }
    }

    [Authorize(Roles = "Cliente")]
    [HttpGet("mi-reserva/{id:int}")]
    public async Task<IActionResult> MiReserva(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var jwt = Jwt();
        if (jwt is null) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            var (status, estado) = await LeerAsync<EstadoPagoCliente>($"api/MiCuenta/reservas/{id}/estado-adelanto", jwt, ct);
            if (status == HttpStatusCode.NotFound) return NotFound();
            if (estado is null) throw new HttpRequestException("No fue posible leer la reserva.");
            var (_, items) = await LeerAsync<List<ComprobanteListaItem>>($"{Base}/mis-reservas/{id}", jwt, ct);
            var model = new AdelantoClienteViewModel
            {
                ReservaId = id, Tour = estado.Tour, EstadoReserva = estado.EstadoReserva,
                Total = estado.Total, Pagado = estado.Pagado, Devuelto = estado.Devuelto,
                PorcentajeAdelanto = estado.PorcentajeAdelanto,
                VencePendienteUtc = estado.VencePendienteUtc,
                Monto = Math.Min(Math.Max(0, estado.Total - (estado.Pagado - estado.Devuelto)),
                    Math.Max(0, Math.Ceiling(estado.Total * estado.PorcentajeAdelanto) / 100m - (estado.Pagado - estado.Devuelto))),
                Comprobantes = items ?? new()
            };
            ViewBag.InstruccionesTransferencia = configuration["Pagos:InstruccionesTransferencia"];
            Response.Headers.CacheControl = "private, no-store";
            return View(model);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No fue posible consultar los adelantos. Intenta nuevamente.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La consulta tardó demasiado. Intenta nuevamente.";
        }
        return RedirectToAction("MisReservas", "MiCuentaWeb");
    }

    [Authorize(Roles = "Cliente")]
    [HttpPost("mi-reserva/{id:int}/reportar")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 3 * 1024 * 1024)]
    public async Task<IActionResult> Reportar(int id, AdelantoClienteViewModel model, CancellationToken ct)
    {
        if (id <= 0 || id != model.ReservaId) return BadRequest();
        if (model.Monto <= 0 || decimal.Round(model.Monto, 2) != model.Monto ||
            string.IsNullOrWhiteSpace(model.Referencia) || model.Referencia.Trim().Length is < 4 or > 100 ||
            model.Archivo is null || model.Archivo.Length is <= 0 or > 2 * 1024 * 1024)
        {
            TempData["Error"] = "Revisa el importe, la referencia y el archivo (PDF, JPG, PNG o WebP de hasta 2 MB).";
            return RedirectToAction(nameof(MiReserva), new { id });
        }
        var jwt = Jwt();
        if (jwt is null) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            await using var buffer = new MemoryStream();
            await model.Archivo.CopyToAsync(buffer, ct);
            if (buffer.Length > 2 * 1024 * 1024) throw new InvalidOperationException();
            using var request = Req(HttpMethod.Post, $"{Base}/mis-reservas/{id}", jwt);
            request.Content = JsonContent.Create(new
            {
                Monto = model.Monto, Referencia = model.Referencia.Trim(),
                ArchivoBase64 = Convert.ToBase64String(buffer.ToArray())
            });
            using var resp = await Api().SendAsync(request, ct);
            TempData[resp.StatusCode == HttpStatusCode.Created ? "Exito" : "Error"] =
                resp.StatusCode switch
                {
                    HttpStatusCode.Created => "Comprobante recibido. La agencia revisará la transferencia. No constituye un pago aprobado.",
                    HttpStatusCode.Conflict => "La reserva está vencida, no está pendiente o ya hay un comprobante por revisar. Consulta el estado.",
                    HttpStatusCode.BadRequest => "El monto, la referencia o el archivo no son válidos.",
                    HttpStatusCode.NotFound => "No se encontró esta reserva en tu cuenta.",
                    _ => $"No se pudo reportar el comprobante (HTTP {(int)resp.StatusCode})."
                };
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No fue posible enviar el archivo. Revisa si el comprobante llegó antes de repetir.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La operación tardó demasiado. Revisa el historial antes de reintentar.";
        }
        catch (InvalidOperationException)
        {
            TempData["Error"] = "La imagen o PDF supera el máximo permitido de 2 MB.";
        }
        return RedirectToAction(nameof(MiReserva), new { id });
    }

    [Authorize(Roles = "Administrador,Operador")]
    [HttpGet("revision")]
    public async Task<IActionResult> Revision(CancellationToken ct)
    {
        var jwt = Jwt();
        if (jwt is null) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            var (_, pendientes) = await LeerAsync<List<ComprobanteListaItem>>($"{Base}/pendientes", jwt, ct);
            Response.Headers.CacheControl = "private, no-store";
            return View(new RevisionComprobanteViewModel { Pendientes = pendientes ?? new() });
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No fue posible cargar la bandeja de revisión.";
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TempData["Error"] = "La API tardó demasiado en responder.";
        }
        return RedirectToAction("Index", "Panel");
    }

    [Authorize(Roles = "Administrador,Operador")]
    [HttpGet("archivo/{id:long}")]
    public async Task<IActionResult> Descargar(long id, CancellationToken ct)
    {
        var jwt = Jwt();
        if (jwt is null) return RedirectToAction("Acceder", "Cuenta");
        using var req = Req(HttpMethod.Get, $"{Base}/{id}/archivo", jwt);
        using var resp = await Api().SendAsync(req, ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return NotFound();
        resp.EnsureSuccessStatusCode();
        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
        var filename = resp.Content.Headers.ContentDisposition?.FileNameStar ??
            resp.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"comprobante-{id}.bin";
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(bytes, "application/octet-stream", Path.GetFileName(filename));
    }

    [Authorize(Roles = "Administrador,Operador")]
    [HttpPost("{id:long}/aprobar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(long id, CancellationToken ct)
        => await ResolverRevision(id, "aprobar", null, ct);

    [Authorize(Roles = "Administrador,Operador")]
    [HttpPost("{id:long}/rechazar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(long id, [FromForm] RechazoComprobanteViewModel model, CancellationToken ct)
    {
        if (model.Id != id || string.IsNullOrWhiteSpace(model.Motivo) ||
            model.Motivo.Trim().Length is < 5 or > 300)
        {
            TempData["Error"] = "Escribe un motivo de rechazo entre 5 y 300 caracteres.";
            return RedirectToAction(nameof(Revision));
        }
        return await ResolverRevision(id, "rechazar", model.Motivo.Trim(), ct);
    }

    private async Task<IActionResult> ResolverRevision(long id, string accion, string? motivo, CancellationToken ct)
    {
        var jwt = Jwt();
        if (jwt is null) return RedirectToAction("Acceder", "Cuenta");
        try
        {
            using var req = Req(HttpMethod.Post, $"{Base}/{id}/{accion}", jwt);
            if (motivo is not null) req.Content = JsonContent.Create(new { Motivo = motivo });
            using var resp = await Api().SendAsync(req, ct);
            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.StatusCode switch
            {
                HttpStatusCode.OK when accion == "aprobar" =>
                    "Transferencia verificada y abono registrado. Ahora revisa si cumple el mínimo para confirmar la reserva.",
                HttpStatusCode.NoContent when accion == "rechazar" => "Comprobante rechazado con motivo registrado.",
                HttpStatusCode.Conflict => "La reserva o el comprobante cambió. Revisa pagos, saldo y estado antes de continuar.",
                HttpStatusCode.NotFound => "No se encontró el comprobante.",
                _ => $"No se pudo completar la revisión (HTTP {(int)resp.StatusCode})."
            };
        }
        catch (HttpRequestException)
        { TempData["Error"] = "No hay conexión con la API. Revisa el estado antes de intentar nuevamente."; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        { TempData["Error"] = "La operación tardó demasiado. Comprueba el registro antes de repetir."; }
        return RedirectToAction(nameof(Revision));
    }
}
