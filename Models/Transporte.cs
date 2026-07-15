using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.Models
{
    public class Transporte
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El tipo de transporte es obligatorio.")]
        [MaxLength(50)]
        public string Tipo { get; set; } = string.Empty;

        [MaxLength(150)]
        public string Descripcion { get; set; } = string.Empty;

        public List<Tour> Tours { get; set; } = new();
    }
}