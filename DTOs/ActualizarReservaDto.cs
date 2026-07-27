using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.DTOs
{
    public class ActualizarReservaDto
    {
        [Required]
        public int ClienteId { get; set; }

        [Required]
        public int TourId { get; set; }

        [Required]
        public int MetodoPagoId { get; set; }

        [Range(1, 100, ErrorMessage = "La cantidad de personas debe ser mayor que cero.")]
        public int CantidadPersonas { get; set; }
    }
}