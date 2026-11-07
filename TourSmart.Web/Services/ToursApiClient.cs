using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TourSmart.Web.Models;

namespace TourSmart.Web.Services;

public sealed class ToursApiClient(HttpClient client) : IToursApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<TourCard>> GetToursAsync(CancellationToken cancellationToken = default)
    {
        var tours = await client.GetFromJsonAsync<List<TourCard>>("api/Tours", JsonOptions, cancellationToken);
        return tours ?? new List<TourCard>();
    }

    public async Task<TourCard?> GetTourAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync($"api/Tours/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var tour = await response.Content.ReadFromJsonAsync<TourCard>(JsonOptions, cancellationToken);
        if (tour is null) return null;
        using var condiciones = await client.GetAsync($"api/Tours/{id}/condiciones", cancellationToken);
        condiciones.EnsureSuccessStatusCode();
        var politica = await condiciones.Content.ReadFromJsonAsync<CondicionesTour>(JsonOptions, cancellationToken);
        if (politica is not null) tour.PorcentajeAdelanto = politica.PorcentajeAdelanto;
        return tour;
    }
}
