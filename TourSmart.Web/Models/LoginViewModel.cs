using System.ComponentModel.DataAnnotations;

namespace TourSmart.Web.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Introduce tu correo.")]
    [EmailAddress(ErrorMessage = "Introduce un correo válido.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Introduce tu contraseña.")]
    [DataType(DataType.Password)]
    public string Contrasena { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
