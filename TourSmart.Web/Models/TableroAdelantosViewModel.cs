namespace TourSmart.Web.Models;

public sealed class IndicadorExcursionComprobantes
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int Total { get; set; }
    public int Pendientes { get; set; }
    public int Aprobados { get; set; }
    public int Rechazados { get; set; }
    public decimal MontoAprobado { get; set; }
}

public sealed class MovimientoComprobanteFinanciero
{
    public long Id { get; set; }
    public int ReservaId { get; set; }
    public DateTime CreadoUtc { get; set; }
    public decimal MontoReportado { get; set; }
    public string Referencia { get; set; } = "";
    public string Estado { get; set; } = "";
    public string? MotivoRevision { get; set; }
    public DateTime? RevisadoUtc { get; set; }
    public int? PagoReservaId { get; set; }
    public string Cliente { get; set; } = "";
    public string Tour { get; set; } = "";
}

public sealed class TableroAdelantosViewModel
{
    public DateTime DesdeUtc { get; set; }
    public DateTime HastaExclusivoUtc { get; set; }
    public DateTime HastaIncluido => HastaExclusivoUtc > DateTime.MinValue ? HastaExclusivoUtc.AddDays(-1) : DateTime.Today;
    public int TotalComprobantes { get; set; }
    public int CantidadPendientes { get; set; }
    public int CantidadAprobados { get; set; }
    public int CantidadRechazados { get; set; }
    public decimal MontoPendienteReportado { get; set; }
    public decimal MontoAprobadoReportado { get; set; }
    public decimal MontoRechazadoReportado { get; set; }
    public int PendientesActualesGlobal { get; set; }
    public int PendientesConPlazoOriginalVencido { get; set; }
    public List<IndicadorExcursionComprobantes> PorExcursion { get; set; } = new();
    public List<MovimientoComprobanteFinanciero> Recientes { get; set; } = new();
}
