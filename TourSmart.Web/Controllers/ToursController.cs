using Microsoft.AspNetCore.Mvc;
using TourSmart.Web.Models;
using TourSmart.Web.Services;

namespace TourSmart.Web.Controllers;

[Route("tours")]
public sealed class ToursController(IToursApiClient toursApi) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? busqueda, string? categoria, int? paisId, int? destinoId, bool soloDisponibles, CancellationToken cancellationToken)
    {
        try
        {
            var tours = await toursApi.GetToursAsync(cancellationToken);
            var filtrados = tours.AsEnumerable();
            if (paisId > 0)
                filtrados = filtrados.Where(t => t.PaisId == paisId);
            if (destinoId > 0)
                filtrados = filtrados.Where(t => t.DestinoId == destinoId);
            if (soloDisponibles)
                filtrados = filtrados.Where(t => t.Reservable);
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                filtrados = filtrados.Where(t =>
                    t.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                    t.Destino.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                    t.Pais.Contains(busqueda, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(categoria))
                filtrados = filtrados.Where(t => string.Equals(t.Categoria, categoria, StringComparison.OrdinalIgnoreCase));

            return View(new CatalogoViewModel
            {
                Tours = filtrados.OrderBy(t => t.Fecha).ToList(),
                Busqueda = busqueda,
                Categoria = categoria,
                PaisId = paisId,
                DestinoId = destinoId,
                SoloDisponibles = soloDisponibles,
                Paises = tours.Where(t => t.PaisId > 0).GroupBy(t => t.PaisId).Select(g => new OpcionCatalogo { Id = g.Key, Nombre = g.First().Pais }).OrderBy(p => p.Nombre).ToList(),
                Destinos = tours.Where(t => t.DestinoId > 0).GroupBy(t => t.DestinoId).Select(g => new DestinoCatalogo { Id = g.Key, PaisId = g.First().PaisId, Nombre = g.First().Destino }).OrderBy(d => d.Nombre).ToList(),
                Categorias = tours.Select(t => t.Categoria).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c).ToList()
            });
        }
        catch (HttpRequestException)
        {
            return View(new CatalogoViewModel { ServicioDisponible = false, Busqueda = busqueda, Categoria = categoria, PaisId = paisId, DestinoId = destinoId, SoloDisponibles = soloDisponibles });
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return View(new CatalogoViewModel { ServicioDisponible = false, Busqueda = busqueda, Categoria = categoria });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id, CancellationToken cancellationToken)
    {
        try
        {
            var tour = await toursApi.GetTourAsync(id, cancellationToken);
            return tour == null ? NotFound() : View(tour);
        }
        catch (HttpRequestException)
        {
            return View("ErrorServicio");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return View("ErrorServicio");
        }
    }
}
