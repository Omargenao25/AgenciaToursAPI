using System.ComponentModel.DataAnnotations;

namespace TourSmart.Web.Models;

public sealed class RestablecerContrasenaViewModel
{
    [Required]
    public string Token { get; set; } = "";

    [Required(ErrorMessage = "Introduce tu nueva contraseña.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Usa al menos 10 caracteres.")]
    [DataType(DataType.Password)]
    public string ContrasenaNueva { get; set; } = "";

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [Compare(nameof(ContrasenaNueva), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmarContrasena { get; set; } = "";
}
