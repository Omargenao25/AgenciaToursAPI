using System.ComponentModel.DataAnnotations;

namespace TourSmart.Web.Models;

public sealed class SolicitarRecuperacionViewModel
{
    [Required(ErrorMessage = "Introduce tu correo.")]
    [EmailAddress(ErrorMessage = "Introduce un correo válido.")]
    [StringLength(150)]
    public string Correo { get; set; } = "";
}
