using TourSmart.Web.Models;
using System.Net;

namespace TourSmart.Web.Services;

public interface IReservasAdminClient
{
    Task<ReporteFinancieroViewModel> ReporteFinancieroAsync(string jwt, DateTime desde, DateTime hasta, CancellationToken ct);
    Task<IReadOnlyList<ReembolsoReservaItem>> ReembolsosAsync(string jwt, int reservaId, CancellationToken ct);
    Task<(HttpStatusCode Estado, string Mensaje)> RegistrarReembolsoAsync(string jwt, RegistrarReembolsoViewModel model, CancellationToken ct);
    Task<EstadoPagosReserva> EstadoPagosAsync(string jwt, int reservaId, CancellationToken ct);
    Task<(HttpStatusCode Estado, string Mensaje)> RegistrarPagoAsync(string jwt, RegistrarPagoViewModel model, CancellationToken ct);
    Task<IReadOnlyList<ReservaPanel>> ListarAsync(string token, CancellationToken cancellationToken);
    Task<(IReadOnlyList<ClienteReserva> Clientes, IReadOnlyList<MetodoPagoReserva> Metodos)> CatalogosAsync(string token, CancellationToken ct);
    Task<(HttpStatusCode Estado, string Mensaje)> CrearAsync(string token, CrearReservaViewModel model, CancellationToken ct);
    Task<HttpStatusCode> CambiarEstadoAsync(string token, int reservaId, string accion, CancellationToken cancellationToken);
}
