namespace AgenciaToursAPI.DTOs
{
    public class TourRespuestaDto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public int PaisId { get; set; }
        public int DestinoId { get; set; }
        public int CategoriaId { get; set; }
        public int GuiaId { get; set; }
        public int TransporteId { get; set; }

        public string Pais { get; set; } = string.Empty;

        public string Destino { get; set; } = string.Empty;

        public string Categoria { get; set; } = string.Empty;

        public string Guia { get; set; } = string.Empty;

        public string Transporte { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        public TimeSpan Hora { get; set; }

        public decimal Precio { get; set; }

        public decimal Itbis { get; set; }

        public int DuracionDias { get; set; }

        public int DuracionHoras { get; set; }

        public DateTime FechaFin { get; set; }

        public string Estado { get; set; } = string.Empty;
        public int CapacidadMaxima { get; set; }
        public bool CerradoOperativo { get; set; }
        public int CuposReservados { get; set; }
        public int CuposDisponibles => Math.Max(0, CapacidadMaxima - CuposReservados);
    }
}