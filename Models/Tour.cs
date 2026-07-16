using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenciaToursAPI.Models
{
    public class Tour
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del tour es obligatorio.")]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un país.")]
        public int PaisId { get; set; }

        public Pais? Pais { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un destino.")]
        public int DestinoId { get; set; }

        public Destino? Destino { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una categoría.")]
        public int CategoriaId { get; set; }

        public Categoria? Categoria { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un guía.")]
        public int GuiaId { get; set; }

        public Guia? Guia { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un transporte.")]
        public int TransporteId { get; set; }

        public Transporte? Transporte { get; set; }

        [Required(ErrorMessage = "La fecha del tour es obligatoria.")]
        public DateTime Fecha { get; set; }

        [Required(ErrorMessage = "La hora del tour es obligatoria.")]
        public TimeSpan Hora { get; set; }

        [Range(0.01, 10000000, ErrorMessage = "El precio debe ser mayor que cero.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Itbis { get; set; }

        public int DuracionDias { get; set; }

        public int DuracionHoras { get; set; }

        public DateTime FechaFin { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = string.Empty;

        public List<Reserva> Reservas { get; set; } = new();
    }
}