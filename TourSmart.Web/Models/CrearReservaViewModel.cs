using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TourSmart.Web.Models;

public sealed class CrearReservaViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un cliente.")]
    public int ClienteId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un tour.")]
    public int TourId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un método de pago.")]
    public int MetodoPagoId { get; set; }

    [Range(1, 100, ErrorMessage = "La reserva debe tener entre 1 y 100 personas.")]
    public int CantidadPersonas { get; set; } = 1;

    public IReadOnlyList<SelectListItem> Clientes { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Tours { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> MetodosPago { get; set; } = Array.Empty<SelectListItem>();
}
