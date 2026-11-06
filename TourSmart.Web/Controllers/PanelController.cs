using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TourSmart.Web.Controllers;

[Authorize]
[Route("panel")]
public sealed class PanelController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
