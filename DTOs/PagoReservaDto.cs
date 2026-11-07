using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.DTOs;

public sealed class RegistrarPagoReservaDto
{
    [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "El monto debe ser positivo.")]
    public decimal Monto { get; set; }
    [Range(1, int.MaxValue)]
    public int MetodoPagoId { get; set; }
    [StringLength(100)]
    public string Referencia { get; set; } = "";
    [StringLength(200)]
    public string Observacion { get; set; } = "";
    [Required]
    public Guid ClaveOperacion { get; set; }
}

public sealed class PagoReservaRespuestaDto
{
    public int Id { get; set; }
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = "";
    public DateTime FechaRegistroUtc { get; set; }
    public string Referencia { get; set; } = "";
    public string Observacion { get; set; } = "";
    public string RegistradoPor { get; set; } = "";
}

public sealed class EstadoPagosReservaDto
{
    public int ReservaId { get; set; }
    public string Cliente { get; set; } = "";
    public string Tour { get; set; } = "";
    public string EstadoReserva { get; set; } = "";
    public DateTime? FechaTour { get; set; }
    public TimeSpan? HoraTour { get; set; }
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public int PorcentajeAdelanto { get; set; }
    public decimal AdelantoMinimo => Math.Ceiling(Total * PorcentajeAdelanto) / 100m;
    public decimal FaltanteParaConfirmar => Math.Max(0m, AdelantoMinimo - Pagado);
    public bool PuedeConfirmar => EstadoReserva == "Pendiente" && Pagado >= AdelantoMinimo &&
        FechaTour.HasValue && HoraTour.HasValue && DateTime.Now < FechaTour.Value.Date.Add(HoraTour.Value);
    public decimal Saldo => Math.Max(0m, Total - Pagado);
    public string EstadoFinanciero => Pagado <= 0 ? "Sin pagos" : Pagado >= Total ? "Pagada" : "Pago parcial";
    public List<PagoReservaRespuestaDto> Pagos { get; set; } = new();
}
