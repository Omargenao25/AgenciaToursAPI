using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AgenciaToursAPI.Models
{
    public class Destino
    {
        public int Id { get; set; }

        [MaxLength(500)]
        [RegularExpression(@"^(?:https://[^\s/?#@:]+(?:\:[0-9]{2,5})?(?:[/\?#][^\s]*)?|/uploads/tours/[a-f0-9]{32}\.(?:jpg|png|webp))$", ErrorMessage = "Usa HTTPS o una foto subida desde tu equipo.")]
        public string? ImagenUrl { get; set; }

        [Required(ErrorMessage = "El nombre del destino es obligatorio.")]
        [MaxLength(100,ErrorMessage = "El nombre del destino no puede superar los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Range( 0, 365, ErrorMessage = "La duración en días debe estar entre 0 y 365.")]
        public int DuracionDias { get; set; }

        [Range(0,23,ErrorMessage = "La duración en horas debe estar entre 0 y 23.")]
        public int DuracionHoras { get; set; }

        [Range(1,int.MaxValue,ErrorMessage = "Debe seleccionar un país válido.")]
        public int PaisId { get; set; }

        public Pais? Pais { get; set; }

        [JsonIgnore]
        public List<Tour> Tours { get; set; } = new();
    }
}