using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.DTOs
{
    public class CrearUsuarioDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(
            100,
            MinimumLength = 3,
            ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres."
        )]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(150)]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "La contraseña debe tener al menos 8 caracteres."
        )]
        public string Contraseña { get; set; } = string.Empty;
    }
}