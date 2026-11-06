using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public sealed class ReservasAdminClient(HttpClient client) : IReservasAdminClient
{
    public async Task<IReadOnlyList<ReservaPanel>> ListarAsync(string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/Reservas");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ReservaPanel>>(cancellationToken)
            ?? new List<ReservaPanel>();
    }
}
