using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public sealed class ClientesAdminClient(HttpClient client) : IClientesAdminClient
{
    private static HttpRequestMessage Solicitud(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public async Task<IReadOnlyList<ClienteFormularioViewModel>> ListarAsync(string token, CancellationToken ct)
    {
        using var request = Solicitud(HttpMethod.Get, "api/Clientes", token);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ClienteFormularioViewModel>>(cancellationToken: ct)
               ?? new List<ClienteFormularioViewModel>();
    }

    public async Task<ClienteFormularioViewModel?> ObtenerAsync(string token, int id, CancellationToken ct)
    {
        using var request = Solicitud(HttpMethod.Get, $"api/Clientes/{id}", token);
        using var response = await client.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ClienteFormularioViewModel>(cancellationToken: ct);
    }

    public async Task<(HttpStatusCode Estado, string? Mensaje)> GuardarAsync(
        string token, ClienteFormularioViewModel cliente, CancellationToken ct)
    {
        var nuevo = cliente.Id == 0;
        using var request = Solicitud(nuevo ? HttpMethod.Post : HttpMethod.Put,
            nuevo ? "api/Clientes" : $"api/Clientes/{cliente.Id}", token);
        request.Content = JsonContent.Create(new
        {
            cliente.Id, cliente.Nombre, cliente.Apellido, cliente.Cedula,
            cliente.Telefono, cliente.Correo
        });
        using var response = await client.SendAsync(request, ct);
        var mensaje = response.IsSuccessStatusCode ? null : await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, mensaje);
    }
}
