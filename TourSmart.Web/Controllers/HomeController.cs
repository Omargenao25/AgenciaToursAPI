using Microsoft.AspNetCore.Mvc;
using TourSmart.Web.Models;
using TourSmart.Web.Services;

namespace TourSmart.Web.Controllers;

public sealed class HomeController(IToursApiClient toursApi) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var tours = await toursApi.GetToursAsync(cancellationToken);
            return View(new CatalogoViewModel
            {
                Tours = tours.Where(t => t.Reservable).OrderBy(t => t.Fecha).Take(3).ToList()
            });
        }
        catch (HttpRequestException)
        {
            return View(new CatalogoViewModel { ServicioDisponible = false });
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return View(new CatalogoViewModel { ServicioDisponible = false });
        }
    }

    [Route("Home/Error")]
    public IActionResult Error() => View();
}
