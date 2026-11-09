using System.Net;
using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public interface IToursAdminClient
{
    Task<HttpStatusCode> MarcarAsistenciaAsync(string token, int tourId, int pasajeroId, bool presente, CancellationToken ct);
    Task<EditarPasajerosViewModel> ObtenerPasajerosAsync(string token, int reservaId, CancellationToken ct);
    Task<HttpStatusCode> ActualizarPasajerosAsync(string token, EditarPasajerosViewModel model, CancellationToken ct);
    Task<ManifiestoTourViewModel> ObtenerManifiestoAsync(string token, int tourId, CancellationToken ct);
    Task<AdelantoTourViewModel> ObtenerAdelantoAsync(string token, int id, CancellationToken ct);
    Task<HttpStatusCode> ConfigurarAdelantoAsync(string token, AdelantoTourViewModel model, CancellationToken ct);
    Task<TourAdminCatalogos> CatalogosAsync(string token, CancellationToken ct);
    Task<HttpStatusCode> GuardarAsync(string token, TourFormularioViewModel model, CancellationToken ct);
}
