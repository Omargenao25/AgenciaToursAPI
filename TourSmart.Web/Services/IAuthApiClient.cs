using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public interface IAuthApiClient
{
    Task<RespuestaLogin?> LoginAsync(string correo, string contrasena, CancellationToken cancellationToken);
}
