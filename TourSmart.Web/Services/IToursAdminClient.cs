using System.Net;
using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public interface IToursAdminClient
{
    Task<TourAdminCatalogos> CatalogosAsync(string token, CancellationToken ct);
    Task<HttpStatusCode> GuardarAsync(string token, TourFormularioViewModel model, CancellationToken ct);
}
