using System.ComponentModel.DataAnnotations;
using AgenciaToursAPI.Security;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenciaToursAPI.Models
{
    public class Reserva
    {
        public int Id { get; set; }

        [Range( 1, int.MaxValue, ErrorMessage = "Debe seleccionar un cliente válido.")]
        public int ClienteId { get; set; }

        public Cliente? Cliente { get; set; }

        [Range( 1, int.MaxValue, ErrorMessage = "Debe seleccionar un tour válido.")]
        public int TourId { get; set; }

        public Tour? Tour { get; set; }

        [Range( 1, int.MaxValue, ErrorMessage = "Debe seleccionar un método de pago válido.")]
        public int MetodoPagoId { get; set; }

        public MetodoPago? MetodoPago { get; set; }

        [Required]
        [MaxLength(20)]
        public string Estado { get; set; } = EstadosReserva.Pendiente;

        public DateTime? FechaCambioEstado { get; set; }

        public DateTime FechaReserva { get; set; }

        // UTC; null para reservas históricas anteriores a esta funcionalidad.
        public DateTime? VencePendienteUtc { get; set; }

        [MaxLength(30)]
        public string? MotivoCancelacion { get; set; }

        [Range( 1, 100, ErrorMessage = "La cantidad de personas debe estar entre 1 y 100.")]
        public int CantidadPersonas { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
    }
}