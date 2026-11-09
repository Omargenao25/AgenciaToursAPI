namespace TourSmart.Web.Models;

public sealed class TourCard
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int PaisId { get; set; }
    public int DestinoId { get; set; }
    public int CategoriaId { get; set; }
    public int GuiaId { get; set; }
    public int TransporteId { get; set; }
    public string Pais { get; set; } = "";
    public string Destino { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Guia { get; set; } = "";
    public string Transporte { get; set; } = "";
    public DateTime Fecha { get; set; }
    public TimeSpan Hora { get; set; }
    public decimal Precio { get; set; }
    public int PorcentajeAdelanto { get; set; } = 30;
    public int HorasLimiteAdelanto { get; set; } = 24;
    public int DuracionDias { get; set; }
    public int DuracionHoras { get; set; }
    public string Estado { get; set; } = "";
    public int CapacidadMaxima { get; set; }
    public bool CerradoOperativo { get; set; }
    public int CuposReservados { get; set; }
    public int CuposDisponibles { get; set; }
    public bool Reservable => !CerradoOperativo && Estado == "Disponible" && CuposDisponibles > 0;
}
