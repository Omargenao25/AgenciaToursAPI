using System.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;
using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public sealed class ReservasAdminClient(HttpClient client) : IReservasAdminClient
{
    public async Task<ReporteFinancieroViewModel> ReporteFinancieroAsync(string jwt, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var url = $"api/reportes/finanzas?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ReporteFinancieroViewModel>(cancellationToken: ct)
            ?? throw new HttpRequestException("No se recibió el reporte financiero.");
    }

    public async Task<IReadOnlyList<ReembolsoReservaItem>> ReembolsosAsync(string jwt, int reservaId, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"api/reservas/{reservaId}/reembolsos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        using var resp = await client.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<ReembolsoReservaItem>>(cancellationToken: ct)
            ?? new List<ReembolsoReservaItem>();
    }

    public async Task<(HttpStatusCode Estado, string Mensaje)> RegistrarReembolsoAsync(string jwt, RegistrarReembolsoViewModel model, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"api/reservas/{model.ReservaId}/reembolsos")
        {
            Content = JsonContent.Create(new {
                model.Monto, model.MetodoPagoId, model.Motivo,
                model.Referencia, model.Observacion, model.ClaveOperacion
            })
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        using var resp = await client.SendAsync(req, ct);
        return (resp.StatusCode, resp.IsSuccessStatusCode ? "" : await resp.Content.ReadAsStringAsync(ct));
    }

    public async Task<EstadoPagosReserva> EstadoPagosAsync(string jwt, int reservaId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/reservas/{reservaId}/pagos");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EstadoPagosReserva>(cancellationToken: ct)
            ?? throw new HttpRequestException("No se recibieron los datos del saldo.");
    }

    public async Task<(HttpStatusCode Estado, string Mensaje)> RegistrarPagoAsync(string jwt, RegistrarPagoViewModel model, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/reservas/{model.ReservaId}/pagos")
        {
            Content = JsonContent.Create(new {
                model.Monto, model.MetodoPagoId, model.Referencia,
                model.Observacion, model.ClaveOperacion
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        using var response = await client.SendAsync(request, ct);
        var message = response.IsSuccessStatusCode ? "" : await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, message);
    }

    public async Task<IReadOnlyList<ReservaPanel>> ListarAsync(string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/Reservas");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ReservaPanel>>(cancellationToken)
            ?? new List<ReservaPanel>();
    }

    private async Task<IReadOnlyList<T>> ObtenerCatalogoAsync<T>(string token, string route, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<T>>(cancellationToken: ct) ?? new List<T>();
    }

    public async Task<(IReadOnlyList<ClienteReserva> Clientes, IReadOnlyList<MetodoPagoReserva> Metodos)> CatalogosAsync(
        string token, CancellationToken ct)
    {
        var clientes = await ObtenerCatalogoAsync<ClienteReserva>(token, "api/Clientes", ct);
        var metodos = await ObtenerCatalogoAsync<MetodoPagoReserva>(token, "api/MetodosPago", ct);
        return (clientes, metodos);
    }

    public async Task<(HttpStatusCode Estado, string Mensaje)> CrearAsync(
        string token, CrearReservaViewModel model, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/Reservas")
        {
            Content = JsonContent.Create(new
            {
                model.ClienteId, model.TourId, model.MetodoPagoId, model.CantidadPersonas
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, ct);
        var message = response.IsSuccessStatusCode ? "" : await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, message);
    }

    public async Task<HttpStatusCode> CambiarEstadoAsync(
        string token, int reservaId, string accion, CancellationToken cancellationToken)
    {
        if (reservaId <= 0) throw new ArgumentOutOfRangeException(nameof(reservaId));
        if (accion is not ("confirmar" or "cancelar")) throw new ArgumentException("Acción no permitida.", nameof(accion));

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/Reservas/{reservaId}/{accion}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        return response.StatusCode;
    }
}
