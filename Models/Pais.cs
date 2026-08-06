using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AgenciaToursAPI.Models
{
    public class Pais
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del país es obligatorio.")]
        [MaxLength(
            100,
            ErrorMessage = "El nombre del país no puede superar los 100 caracteres."
        )]
        public string Nombre { get; set; } = string.Empty;

        [JsonIgnore]
        public List<Destino> Destinos { get; set; } = new();
    }
}