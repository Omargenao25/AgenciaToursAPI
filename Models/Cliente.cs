using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AgenciaToursAPI.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
        [MaxLength(80,ErrorMessage = "El nombre del cliente no puede superar los 80 caracteres.")]
        public string Nombre { get; set; } = string.Empty;


        [Required(ErrorMessage = "El apellido del cliente es obligatorio.")]
        [MaxLength( 80, ErrorMessage = "El apellido del cliente no puede superar los 80 caracteres.")]
        public string Apellido { get; set; } = string.Empty;


        [Required(ErrorMessage = "La cédula es obligatoria.")]
        [MaxLength(20,ErrorMessage = "La cédula no puede superar los 20 caracteres.")]
        public string Cedula { get; set; } = string.Empty;


        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [MaxLength(20,ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
        public string Telefono { get; set; } = string.Empty;


        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [MaxLength(150,ErrorMessage = "El correo no puede superar los 150 caracteres.")]
        public string Correo { get; set; } = string.Empty;


        [JsonIgnore]
        public List<Reserva> Reservas { get; set; } = new();
    }
}