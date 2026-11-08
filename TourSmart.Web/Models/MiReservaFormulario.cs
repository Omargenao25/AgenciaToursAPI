using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TourSmart.Web.Models;

public sealed class MiReservaFormulario
{
    [Range(1, int.MaxValue)] public int TourId { get; set; }
    [Range(1, int.MaxValue)] public int MetodoPagoId { get; set; }
    [Range(1, 100, ErrorMessage = "Elige de 1 a 100 personas.")]
    public int CantidadPersonas { get; set; } = 1;
    public List<string> Pasajeros { get; set; } = new();
    public TourCard? Tour { get; set; }
    public IReadOnlyList<SelectListItem> Metodos { get; set; } = Array.Empty<SelectListItem>();
}
