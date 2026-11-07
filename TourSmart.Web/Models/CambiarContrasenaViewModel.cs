using System.ComponentModel.DataAnnotations;

namespace TourSmart.Web.Models;

public sealed class CambiarContrasenaViewModel
{
    [Required(ErrorMessage = "Introduce tu contraseña actual.")]
    [DataType(DataType.Password)]
    public string ContrasenaActual { get; set; } = "";

    [Required(ErrorMessage = "Introduce tu nueva contraseña.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Usa de 10 a 100 caracteres.")]
    [DataType(DataType.Password)]
    public string ContrasenaNueva { get; set; } = "";

    [Required(ErrorMessage = "Confirma tu nueva contraseña.")]
    [Compare(nameof(ContrasenaNueva), ErrorMessage = "Las contraseñas nuevas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmarContrasena { get; set; } = "";
}
