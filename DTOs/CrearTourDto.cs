using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.DTOs
{
    public class CrearTourDto
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;

        public int PaisId { get; set; }

        public int DestinoId { get; set; }

        public int CategoriaId { get; set; }

        public int GuiaId { get; set; }

        public int TransporteId { get; set; }

        public DateTime Fecha { get; set; }

        public TimeSpan Hora { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Precio { get; set; }
    }
}