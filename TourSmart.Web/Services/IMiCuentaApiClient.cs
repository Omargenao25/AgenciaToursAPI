using System.Net;
using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public interface IMiCuentaApiClient
{
    Task<(HttpStatusCode Estado, string Mensaje)> CambiarContrasenaAsync(string jwt, CambiarContrasenaViewModel model, CancellationToken ct);
    Task<MiPerfilViewModel> PerfilAsync(string jwt, CancellationToken ct);
    Task<HttpStatusCode> ActualizarPerfilAsync(string jwt, MiPerfilViewModel perfil, CancellationToken ct);
    Task<HttpStatusCode> RegistrarAsync(RegistroViajeroViewModel model, CancellationToken ct);
    Task<IReadOnlyList<ReservaPanel>> MisReservasAsync(string jwt, CancellationToken ct);
    Task<IReadOnlyList<MetodoPagoReserva>> MetodosAsync(string jwt, CancellationToken ct);
    Task<(HttpStatusCode Estado, string Mensaje)> CancelarAsync(string jwt, int reservaId, CancellationToken ct);
    Task<(HttpStatusCode Estado, string Mensaje)> ReservarAsync(string jwt, MiReservaFormulario model, CancellationToken ct);
}
