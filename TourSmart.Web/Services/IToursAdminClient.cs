using System.Net;
using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public interface IToursAdminClient
{
    Task<AdelantoTourViewModel> ObtenerAdelantoAsync(string token, int id, CancellationToken ct);
    Task<HttpStatusCode> ConfigurarAdelantoAsync(string token, AdelantoTourViewModel model, CancellationToken ct);
    Task<TourAdminCatalogos> CatalogosAsync(string token, CancellationToken ct);
    Task<HttpStatusCode> GuardarAsync(string token, TourFormularioViewModel model, CancellationToken ct);
}
