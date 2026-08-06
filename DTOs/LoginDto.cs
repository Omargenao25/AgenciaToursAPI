using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength( 150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
        public string Correo { get; set; } = string.Empty;


        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Contraseña { get; set; } = string.Empty;
    }
}