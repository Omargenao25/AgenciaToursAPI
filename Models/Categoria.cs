using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AgenciaToursAPI.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
        [MaxLength(
            50,
            ErrorMessage = "El nombre de la categoría no puede superar los 50 caracteres."
        )]
        public string Nombre { get; set; } = string.Empty;

        [JsonIgnore]
        public List<Tour> Tours { get; set; } = new();
    }
}