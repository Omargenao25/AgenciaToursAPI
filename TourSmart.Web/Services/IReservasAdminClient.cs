using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public interface IReservasAdminClient
{
    Task<IReadOnlyList<ReservaPanel>> ListarAsync(string token, CancellationToken cancellationToken);
}
