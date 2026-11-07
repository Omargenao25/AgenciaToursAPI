using System.Net;
using System.Net.Http.Json;
using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public sealed class AuthApiClient(HttpClient client) : IAuthApiClient
{
    public async Task<HttpStatusCode> SolicitarRecuperacionAsync(string correo, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync("api/Recuperacion/solicitar",
            new { Correo = correo.Trim() }, ct);
        return response.StatusCode;
    }

    public async Task<HttpStatusCode> RestablecerAsync(string token, string nueva, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync("api/Recuperacion/restablecer",
            new { Token = token, ContrasenaNueva = nueva }, ct);
        return response.StatusCode;
    }

    public async Task<RespuestaLogin?> LoginAsync(string correo, string contrasena, CancellationToken cancellationToken)
    {
        using var respuesta = await client.PostAsJsonAsync("api/Auth/login",
            new { correo = correo.Trim(), contraseña = contrasena }, cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            return null;

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<RespuestaLogin>(cancellationToken);
    }
}
