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

        // Marca persistente del intento de recordatorio, para evitar duplicados.
        public DateTime? RecordatorioIntentadoUtc { get; set; }
        public DateTime? RecordatorioEnviadoUtc { get; set; }
        
        // Fase 5.25: estado del despacho y lease persistente de una sola instancia.
        [MaxLength(20)]
        public string RecordatorioEstado { get; set; } = "Pendiente";
        public int RecordatorioIntentos { get; set; }
        public DateTime? RecordatorioProximoIntentoUtc { get; set; }
        public DateTime? RecordatorioLeaseHastaUtc { get; set; }
        [MaxLength(32)]
        public string? RecordatorioLeaseToken { get; set; }

        [MaxLength(30)]
        public string? MotivoCancelacion { get; set; }

        [Range( 1, 100, ErrorMessage = "La cantidad de personas debe estar entre 1 y 100.")]
        public int CantidadPersonas { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
    }
}