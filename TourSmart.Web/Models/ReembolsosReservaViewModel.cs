using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TourSmart.Web.Models;

public sealed class ReembolsoReservaItem
{
    public int Id { get; set; }
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = "";
    public DateTime FechaRegistroUtc { get; set; }
    public string Referencia { get; set; } = "";
    public string Motivo { get; set; } = "";
    public string Observacion { get; set; } = "";
    public string RegistradoPor { get; set; } = "";
}

public sealed class RegistrarReembolsoViewModel
{
    public int ReservaId { get; set; }
    [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "El importe debe ser positivo.")]
    public decimal Monto { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un método.")]
    public int MetodoPagoId { get; set; }
    [Required(ErrorMessage = "Introduce el motivo."), StringLength(200, MinimumLength = 8)]
    public string Motivo { get; set; } = "";
    [StringLength(100)]
    public string Referencia { get; set; } = "";
    [StringLength(200)]
    public string Observacion { get; set; } = "";
    public Guid ClaveOperacion { get; set; } = Guid.NewGuid();

    public EstadoPagosReserva Resumen { get; set; } = new();
    public IReadOnlyList<ReembolsoReservaItem> Reembolsos { get; set; } = Array.Empty<ReembolsoReservaItem>();
    public List<SelectListItem> Metodos { get; set; } = new();
    public decimal Devuelto => Reembolsos.Sum(x => x.Monto);
    public decimal DisponibleParaDevolver => Math.Max(0m, Resumen.Pagado - Devuelto);
}
