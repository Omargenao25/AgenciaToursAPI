using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.DTOs
{
    public class CrearReservaDto
    {
        [Range(1,int.MaxValue, ErrorMessage = "Debe seleccionar un cliente válido.")]
        public int ClienteId { get; set; }


        [Range(1,int.MaxValue, ErrorMessage = "Debe seleccionar un tour válido.")]
        public int TourId { get; set; }


        [Range( 1, int.MaxValue, ErrorMessage = "Debe seleccionar un método de pago válido.")]
        public int MetodoPagoId { get; set; }


        [Range( 1,100, ErrorMessage = "La cantidad de personas debe estar entre 1 y 100.")]
        public int CantidadPersonas { get; set; }
        public List<string> Pasajeros { get; set; } = new();
    }
}