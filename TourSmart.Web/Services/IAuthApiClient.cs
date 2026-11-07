using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public interface IAuthApiClient
{
    Task<System.Net.HttpStatusCode> SolicitarRecuperacionAsync(string correo, CancellationToken ct);
    Task<System.Net.HttpStatusCode> RestablecerAsync(string token, string nueva, CancellationToken ct);
    Task<RespuestaLogin?> LoginAsync(string correo, string contrasena, CancellationToken cancellationToken);
}
