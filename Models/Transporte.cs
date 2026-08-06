using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AgenciaToursAPI.Models
{
    public class Transporte
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El tipo de transporte es obligatorio.")]
        [MaxLength( 50, ErrorMessage = "El tipo de transporte no puede superar los 50 caracteres.")]
        public string Tipo { get; set; } = string.Empty;


        [MaxLength( 150, ErrorMessage = "La descripción no puede superar los 150 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [JsonIgnore]
        public List<Tour> Tours { get; set; } = new();
    }
}