using Microsoft.AspNetCore.Mvc;
using TourSmart.Web.Models;
using TourSmart.Web.Services;

namespace TourSmart.Web.Controllers;

[Route("tours")]
public sealed class ToursController(IToursApiClient toursApi) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? busqueda, string? categoria, CancellationToken cancellationToken)
    {
        try
        {
            var tours = await toursApi.GetToursAsync(cancellationToken);
            var filtrados = tours.AsEnumerable();
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
                Categoria = categoria
            });
        }
        catch (HttpRequestException)
        {
            return View(new CatalogoViewModel { ServicioDisponible = false, Busqueda = busqueda, Categoria = categoria });
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
