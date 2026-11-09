using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public sealed class ToursAdminClient(HttpClient http) : IToursAdminClient
{
    private async Task<List<T>> ObtenerAsync<T>(string ruta, string token, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, ruta);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<T>>(cancellationToken: ct) ?? new();
    }

    public async Task<HttpStatusCode> MarcarAsistenciaAsync(
        string token, int tourId, int pasajeroId, bool presente, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put,
            $"api/tours/{tourId}/check-in/pasajeros/{pasajeroId}") {
            Content = JsonContent.Create(new { Presente = presente })
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await http.SendAsync(req, ct);
        return resp.StatusCode;
    }

    public async Task<EditarPasajerosViewModel> ObtenerPasajerosAsync(string token, int reservaId, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"api/reservas/{reservaId}/pasajeros");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<EditarPasajerosViewModel>(cancellationToken: ct)
            ?? throw new HttpRequestException("No se recibieron los pasajeros.");
    }

    public async Task<HttpStatusCode> ActualizarPasajerosAsync(string token, EditarPasajerosViewModel model, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, $"api/reservas/{model.Id}/pasajeros") {
            Content = JsonContent.Create(new { model.Pasajeros })
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await http.SendAsync(req, ct);
        return resp.StatusCode;
    }

    public async Task<ManifiestoTourViewModel> ObtenerManifiestoAsync(string token, int tourId, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"api/tours/{tourId}/manifiesto");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ManifiestoTourViewModel>(cancellationToken: ct)
            ?? throw new HttpRequestException("No se recibió el manifiesto del tour.");
    }

    public async Task<AdelantoTourViewModel> ObtenerAdelantoAsync(string token, int id, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"api/Tours/{id}/adelanto");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<AdelantoTourViewModel>(cancellationToken: ct)
            ?? throw new HttpRequestException("No se recibió la política de adelanto.");
    }

    public async Task<HttpStatusCode> ConfigurarAdelantoAsync(string token, AdelantoTourViewModel model, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, $"api/Tours/{model.Id}/adelanto")
        {
            Content = JsonContent.Create(new { model.PorcentajeAdelanto, model.HorasLimiteAdelanto })
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await http.SendAsync(req, ct);
        return resp.StatusCode;
    }

    public async Task<TourAdminCatalogos> CatalogosAsync(string token, CancellationToken ct) => new()
    {
        Paises = await ObtenerAsync<OpcionCatalogo>("api/Paises", token, ct),
        Destinos = await ObtenerAsync<DestinoCatalogo>("api/Destinos", token, ct),
        Categorias = await ObtenerAsync<OpcionCatalogo>("api/Categorias", token, ct),
        Guias = await ObtenerAsync<OpcionCatalogo>("api/Guias", token, ct),
        Transportes = await ObtenerAsync<TransporteCatalogo>("api/Transportes", token, ct)
    };

    public async Task<HttpStatusCode> GuardarAsync(string token, TourFormularioViewModel model, CancellationToken ct)
    {
        var payload = new
        {
            model.Nombre, model.PaisId, model.DestinoId, model.CategoriaId, model.GuiaId,
            model.TransporteId, model.Fecha, model.Hora, model.Precio, model.CapacidadMaxima
        };
        var method = model.Id == 0 ? HttpMethod.Post : HttpMethod.Put;
        var route = model.Id == 0 ? "api/Tours" : $"api/Tours/{model.Id}";
        using var req = new HttpRequestMessage(method, route)
        {
            Content = JsonContent.Create(payload)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await http.SendAsync(req, ct);
        return resp.StatusCode;
    }
}
