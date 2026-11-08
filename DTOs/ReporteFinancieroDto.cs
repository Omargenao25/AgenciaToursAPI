namespace AgenciaToursAPI.DTOs;

public sealed class MovimientoFinancieroDto
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

public sealed class DesgloseFinancieroDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal Cobrado { get; set; }
    public decimal Devuelto { get; set; }
    public decimal Neto => Cobrado - Devuelto;
    public int CantidadPagos { get; set; }
    public int CantidadReembolsos { get; set; }
}

public sealed class ReporteFinancieroDto
{
    public DateTime DesdeUtc { get; set; }
    public DateTime HastaExclusivoUtc { get; set; }
    public decimal TotalCobrado { get; set; }
    public decimal TotalDevuelto { get; set; }
    public decimal Neto => TotalCobrado - TotalDevuelto;
    public int CantidadPagos { get; set; }
    public int CantidadReembolsos { get; set; }
    public int MovimientosTotales => CantidadPagos + CantidadReembolsos;
    public int LimiteListado { get; set; } = 500;
    public bool ListadoTruncado => MovimientosTotales > Movimientos.Count;
    public List<MovimientoFinancieroDto> Movimientos { get; set; } = new();
    public List<DesgloseFinancieroDto> PorTour { get; set; } = new();
    public List<DesgloseFinancieroDto> PorMetodoPago { get; set; } = new();
}
