using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public interface IToursApiClient
{
    Task<IReadOnlyList<TourCard>> GetToursAsync(CancellationToken cancellationToken = default);
    Task<TourCard?> GetTourAsync(int id, CancellationToken cancellationToken = default);
}
