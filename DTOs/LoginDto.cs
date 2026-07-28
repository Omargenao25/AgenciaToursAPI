using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Contraseña { get; set; } = string.Empty;
    }
}