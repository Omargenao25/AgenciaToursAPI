using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
        [MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        public List<Tour> Tours { get; set; } = new();
    }
}