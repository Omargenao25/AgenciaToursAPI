using TourSmart.Web.Models;
using System.Net;

namespace TourSmart.Web.Services;

public interface IReservasAdminClient
{
    Task<IReadOnlyList<ReservaPanel>> ListarAsync(string token, CancellationToken cancellationToken);
    Task<HttpStatusCode> CambiarEstadoAsync(string token, int reservaId, string accion, CancellationToken cancellationToken);
}
