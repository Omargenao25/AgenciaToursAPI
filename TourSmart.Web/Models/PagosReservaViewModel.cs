using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TourSmart.Web.Models;

public sealed class PagoReservaItem
{
    public int Id { get; set; }
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = "";
    public DateTime FechaRegistroUtc { get; set; }
    public string Referencia { get; set; } = "";
    public string Observacion { get; set; } = "";
    public string RegistradoPor { get; set; } = "";
}

public sealed class EstadoPagosReserva
{
    public int ReservaId { get; set; }
    public string Cliente { get; set; } = "";
    public string Tour { get; set; } = "";
    public string EstadoReserva { get; set; } = "";
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public int PorcentajeAdelanto { get; set; }
    public decimal AdelantoMinimo { get; set; }
    public decimal FaltanteParaConfirmar { get; set; }
    public bool PuedeConfirmar { get; set; }
    public decimal Saldo { get; set; }
    public string EstadoFinanciero { get; set; } = "";
    public List<PagoReservaItem> Pagos { get; set; } = new();
}

public sealed class RegistrarPagoViewModel
{
    public int ReservaId { get; set; }
    [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "Introduce un monto positivo.")]
    public decimal Monto { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un método.")]
    public int MetodoPagoId { get; set; }
    [StringLength(100)]
    public string Referencia { get; set; } = "";
    [StringLength(200)]
    public string Observacion { get; set; } = "";
    public Guid ClaveOperacion { get; set; } = Guid.NewGuid();
    public EstadoPagosReserva Resumen { get; set; } = new();
    public List<SelectListItem> Metodos { get; set; } = new();
}
