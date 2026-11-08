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

    public async Task<(HttpStatusCode Estado, string Mensaje)> CambiarContrasenaAsync(
        string jwt, CambiarContrasenaViewModel model, CancellationToken ct)
    {
        using var request = Autorizar(HttpMethod.Post, "api/MiCuenta/cambiar-contrasena", jwt);
        request.Content = JsonContent.Create(new { model.ContrasenaActual, model.ContrasenaNueva });
        using var response = await client.SendAsync(request, ct);
        var mensaje = response.IsSuccessStatusCode ? "" : await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, mensaje);
    }

    public async Task<MiPerfilViewModel> PerfilAsync(string jwt, CancellationToken ct)
    {
        using var request = Autorizar(HttpMethod.Get, "api/MiCuenta/perfil", jwt);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MiPerfilViewModel>(cancellationToken: ct)
            ?? throw new HttpRequestException("Respuesta de perfil vacía.");
    }

    public async Task<HttpStatusCode> ActualizarPerfilAsync(string jwt, MiPerfilViewModel perfil, CancellationToken ct)
    {
        using var request = Autorizar(HttpMethod.Put, "api/MiCuenta/perfil", jwt);
        // Solo se envían campos editables: correo y documento nunca forman parte del PUT.
        request.Content = JsonContent.Create(new { perfil.Nombre, perfil.Apellido, perfil.Telefono });
        using var response = await client.SendAsync(request, ct);
        return response.StatusCode;
    }

    public async Task<ComprobanteReservaViewModel?> ComprobanteAsync(string jwt, int id, CancellationToken ct)
    {
        using var request = Autorizar(HttpMethod.Get, $"api/MiCuenta/reservas/{id}/comprobante", jwt);
        using var response = await client.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ComprobanteReservaViewModel>(cancellationToken: ct);
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

    public async Task<(HttpStatusCode Estado, string Mensaje)> CancelarAsync(
        string jwt, int reservaId, CancellationToken ct)
    {
        using var request = Autorizar(HttpMethod.Patch, $"api/MiCuenta/reservas/{reservaId}/cancelar", jwt);
        using var response = await client.SendAsync(request, ct);
        var mensaje = response.IsSuccessStatusCode ? "" : await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, mensaje);
    }

    public async Task<(HttpStatusCode Estado, string Mensaje)> ReservarAsync(string jwt, MiReservaFormulario model, CancellationToken ct)
    {
        using var request = Autorizar(HttpMethod.Post, "api/MiCuenta/reservas", jwt);
        request.Content = JsonContent.Create(new { model.TourId, model.MetodoPagoId, model.CantidadPersonas, model.Pasajeros });
        using var response = await client.SendAsync(request, ct);
        var mensaje = response.IsSuccessStatusCode ? "" : await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, mensaje);
    }
}
