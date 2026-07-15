using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.Models
{
    public class Guia
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del guía es obligatorio.")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono del guía es obligatorio.")]
        [MaxLength(20)]
        public string Telefono { get; set; } = string.Empty;

        public List<Tour> Tours { get; set; } = new();
    }
}