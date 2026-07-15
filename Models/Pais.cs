using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.Models
{
    public class Pais
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del país es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
        public List<Destino> Destinos { get; set; } = new();
    }
}
