using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AgenciaToursAPI.Models
{
    public class Guia
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del guía es obligatorio.")]
        [MaxLength(100,ErrorMessage = "El nombre del guía no puede superar los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono del guía es obligatorio.")]
        [MaxLength(20,ErrorMessage = "El teléfono del guía no puede superar los 20 caracteres." )]
        public string Telefono { get; set; } = string.Empty;

        [JsonIgnore]
        public List<Tour> Tours { get; set; } = new();
    }
}