using System.Net;
using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public interface IClientesAdminClient
{
    Task<IReadOnlyList<ClienteFormularioViewModel>> ListarAsync(string token, CancellationToken ct);
    Task<ClienteFormularioViewModel?> ObtenerAsync(string token, int id, CancellationToken ct);
    Task<(HttpStatusCode Estado, string? Mensaje)> GuardarAsync(
        string token, ClienteFormularioViewModel cliente, CancellationToken ct);
}
