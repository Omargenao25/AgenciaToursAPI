using System.ComponentModel.DataAnnotations;

namespace TourSmart.Web.Models;

public sealed class VerificarCorreoViewModel
{
    [Required, RegularExpression("^[A-Fa-f0-9]{64}$")]
    public string Token { get; set; } = "";
}

public sealed class ReenviarVerificacionViewModel
{
    [Required(ErrorMessage = "Indica tu dirección de correo.")]
    [EmailAddress(ErrorMessage = "Escribe una dirección de correo válida.")]
    [StringLength(150)]
    public string Correo { get; set; } = "";
}
