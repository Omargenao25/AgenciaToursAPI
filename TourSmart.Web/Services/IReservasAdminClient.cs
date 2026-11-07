using TourSmart.Web.Models;
using System.Net;

namespace TourSmart.Web.Services;

public interface IReservasAdminClient
{
    Task<IReadOnlyList<ReservaPanel>> ListarAsync(string token, CancellationToken cancellationToken);
    Task<(IReadOnlyList<ClienteReserva> Clientes, IReadOnlyList<MetodoPagoReserva> Metodos)> CatalogosAsync(string token, CancellationToken ct);
    Task<(HttpStatusCode Estado, string Mensaje)> CrearAsync(string token, CrearReservaViewModel model, CancellationToken ct);
    Task<HttpStatusCode> CambiarEstadoAsync(string token, int reservaId, string accion, CancellationToken cancellationToken);
}
