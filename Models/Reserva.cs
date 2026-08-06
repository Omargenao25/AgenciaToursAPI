using System.ComponentModel.DataAnnotations;
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

        public DateTime FechaReserva { get; set; }

        [Range( 1, 100, ErrorMessage = "La cantidad de personas debe estar entre 1 y 100.")]
        public int CantidadPersonas { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
    }
}