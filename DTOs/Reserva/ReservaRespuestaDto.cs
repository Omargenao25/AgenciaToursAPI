namespace AgenciaToursAPI.DTOs
{
    public class ReservaRespuestaDto
    {
        public int Id { get; set; }

        public string Cliente { get; set; } = string.Empty;

        public string Tour { get; set; } = string.Empty;
        public int TourId { get; set; }
        public DateTime? FechaTour { get; set; }
        public TimeSpan? HoraTour { get; set; }

        public string MetodoPago { get; set; } = string.Empty;

        public DateTime FechaReserva { get; set; }

        public int CantidadPersonas { get; set; }

        public decimal PrecioPorPersona { get; set; }

        public decimal Total { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaCambioEstado { get; set; }
    }
}