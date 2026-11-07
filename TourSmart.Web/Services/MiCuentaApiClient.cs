using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public sealed class MiCuentaApiClient(HttpClient client) : IMiCuentaApiClient
{
    public async Task<HttpStatusCode> RegistrarAsync(RegistroViajeroViewModel model, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync("api/MiCuenta/registrar", new
        {
            model.Nombre, model.Apellido, model.Correo, model.Telefono,
            model.TipoDocumento, model.NumeroDocumento, model.PaisEmisorDocumento,
            model.Contrasena
        }, ct);
        return response.StatusCode;
    }

    private static HttpRequestMessage Autorizar(HttpMethod method, string route, string jwt)
    {
        var request = new HttpRequestMessage(method, route);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return request;
    }

    public async Task<IReadOnlyList<ReservaPanel>> MisReservasAsync(string jwt, CancellationToken ct)
    {
        using var request = Autorizar(HttpMethod.Get, "api/MiCuenta/reservas", jwt);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ReservaPanel>>(cancellationToken: ct) ?? new();
    }

    public async Task<IReadOnlyList<MetodoPagoReserva>> MetodosAsync(string jwt, CancellationToken ct)
    {
        using var request = Autorizar(HttpMethod.Get, "api/MiCuenta/metodos-pago", jwt);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<MetodoPagoReserva>>(cancellationToken: ct) ?? new();
    }

    public async Task<(HttpStatusCode Estado, string Mensaje)> ReservarAsync(string jwt, MiReservaFormulario model, CancellationToken ct)
    {
        using var request = Autorizar(HttpMethod.Post, "api/MiCuenta/reservas", jwt);
        request.Content = JsonContent.Create(new { model.TourId, model.MetodoPagoId, model.CantidadPersonas });
        using var response = await client.SendAsync(request, ct);
        var mensaje = response.IsSuccessStatusCode ? "" : await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, mensaje);
    }
}
