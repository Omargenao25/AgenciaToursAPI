namespace TourSmart.Web.Models;

public sealed class MovimientoFinanciero
{
    public int Id { get; set; }
    public int ReservaId { get; set; }
    public string Tipo { get; set; } = "";
    public decimal Monto { get; set; }
    public DateTime FechaRegistroUtc { get; set; }
    public string MetodoPago { get; set; } = "";
    public string Referencia { get; set; } = "";
    public string RegistradoPor { get; set; } = "";
    public string Tour { get; set; } = "";
    public string Cliente { get; set; } = "";
}

public sealed class ReporteFinancieroViewModel
{
    public DateTime DesdeUtc { get; set; }
    public DateTime HastaExclusivoUtc { get; set; }
    public decimal TotalCobrado { get; set; }
    public decimal TotalDevuelto { get; set; }
    public decimal Neto { get; set; }
    public int CantidadPagos { get; set; }
    public int CantidadReembolsos { get; set; }
    public int MovimientosTotales { get; set; }
    public int LimiteListado { get; set; } = 500;
    public bool ListadoTruncado { get; set; }
    public List<MovimientoFinanciero> Movimientos { get; set; } = new();
    public DateTime HastaIncluido => HastaExclusivoUtc.AddDays(-1);
}
