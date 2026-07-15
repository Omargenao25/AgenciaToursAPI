using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.Models
{
    public class Destino
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del destino es obligatorio.")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Range(0, 365, ErrorMessage = "La duración en días no puede ser negativa.")]
        public int DuracionDias { get; set; }

        [Range(0, 23, ErrorMessage = "Las horas deben estar entre 0 y 23.")]
        public int DuracionHoras { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un país.")]
        public int PaisId { get; set; }

        public Pais? Pais { get; set; }

        public List<Tour> Tours { get; set; } = new();
    }
}