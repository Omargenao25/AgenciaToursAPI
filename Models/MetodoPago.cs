using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AgenciaToursAPI.Models
{
    public class MetodoPago
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El método de pago es obligatorio.")]
        [MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [JsonIgnore]
        public List<Reserva> Reservas { get; set; } = new();
    }
}