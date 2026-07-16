using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenciaToursAPI.Models
{
    public class Reserva
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un cliente.")]
        public int ClienteId { get; set; }

        public Cliente? Cliente { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un tour.")]
        public int TourId { get; set; }

        public Tour? Tour { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un método de pago.")]
        public int MetodoPagoId { get; set; }

        public MetodoPago? MetodoPago { get; set; }

        public DateTime FechaReserva { get; set; }

        [Range(1, 100, ErrorMessage = "La cantidad de personas debe ser mayor que cero.")]
        public int CantidadPersonas { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
    }
}