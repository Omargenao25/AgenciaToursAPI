using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.DTOs;

public sealed class CambiarContrasenaDto
{
    [Required(ErrorMessage = "Introduce tu contraseña actual.")]
    public string ContrasenaActual { get; set; } = "";

    [Required(ErrorMessage = "Introduce tu nueva contraseña.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "La nueva contraseña debe tener de 10 a 100 caracteres.")]
    public string ContrasenaNueva { get; set; } = "";
}
