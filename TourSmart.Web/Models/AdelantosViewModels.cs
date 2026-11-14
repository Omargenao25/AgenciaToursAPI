using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TourSmart.Web.Models;

public sealed class ComprobanteListaItem
{
    public long Id { get; set; }
    public int ReservaId { get; set; }
    public string Cliente { get; set; } = "";
    public string Tour { get; set; } = "";
    public decimal MontoReportado { get; set; }
    public string Referencia { get; set; } = "";
    public string Estado { get; set; } = "";
    public DateTime CreadoUtc { get; set; }
    public DateTime? RevisadoUtc { get; set; }
    public string? MotivoRevision { get; set; }
    public int? PagoReservaId { get; set; }
}

public sealed class AdelantoClienteViewModel
{
    public int ReservaId { get; set; }
    public string Tour { get; set; } = "";
    public string EstadoReserva { get; set; } = "";
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public decimal Devuelto { get; set; }
    public int PorcentajeAdelanto { get; set; } = 30;
    public DateTime? VencePendienteUtc { get; set; }
    public decimal AdelantoMinimo => Math.Ceiling(Total * PorcentajeAdelanto) / 100m;
    public decimal Saldo => Math.Max(0, Total - (Pagado - Devuelto));
    public decimal FaltanteAdelanto => Math.Max(0, AdelantoMinimo - (Pagado - Devuelto));
    public List<ComprobanteListaItem> Comprobantes { get; set; } = new();
    [Range(typeof(decimal), "0.01", "10000000", ErrorMessage = "Introduce un importe positivo.")]
    public decimal Monto { get; set; }
    [Required, StringLength(100, MinimumLength = 4)]
    public string Referencia { get; set; } = "";
    [Required(ErrorMessage = "Adjunta un comprobante.")]
    public IFormFile? Archivo { get; set; }
}

public sealed class RevisionComprobanteViewModel
{
    public List<ComprobanteListaItem> Pendientes { get; set; } = new();
}

public sealed class RechazoComprobanteViewModel
{
    public long Id { get; set; }
    [Required, StringLength(300, MinimumLength = 5)]
    public string Motivo { get; set; } = "";
}
