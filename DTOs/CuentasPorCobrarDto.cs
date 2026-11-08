namespace AgenciaToursAPI.DTOs;

public sealed class CuentaPorCobrarFilaDto
{
    public int ReservaId { get; set; }
    public int TourId { get; set; }
    public string Tour { get; set; } = "";
    public string Cliente { get; set; } = "";
    public string EstadoReserva { get; set; } = "";
    public DateTime InicioTour { get; set; }
    public DateTime? VenceAdelantoUtc { get; set; }
    public decimal Total { get; set; }
    public decimal Pagos { get; set; }
    public decimal Reembolsos { get; set; }
    public decimal Neto => Pagos - Reembolsos;
    public decimal Saldo => Math.Max(0m, Total - Neto);
    public decimal AdelantoMinimo { get; set; }
    public decimal FaltaAdelanto => Math.Max(0m, AdelantoMinimo - Neto);
    public string Situacion { get; set; } = "";
}

public sealed class AlertaCobroDto
{
    public int ReservaId { get; set; }
    public string Cliente { get; set; } = "";
    public string Tour { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Mensaje { get; set; } = "";
    public string Prioridad { get; set; } = "Media";
    public DateTime InicioTour { get; set; }
    public DateTime? VenceAdelantoUtc { get; set; }
    public decimal ImportePendiente { get; set; }
}

public sealed class CuentasPorCobrarDto
{
    public DateTime ConsultadoUtc { get; set; }
    public int TotalReservasActivas { get; set; }
    public int ReservasConfirmadas { get; set; }
    public int ReservasPendientes { get; set; }
    public decimal TotalSaldoConfirmado { get; set; }
    public decimal TotalSaldoPendienteSinConfirmar { get; set; }
    public decimal TotalSaldoPotencial => TotalSaldoConfirmado + TotalSaldoPendienteSinConfirmar;
    public int RequierenAtencion { get; set; }
    public List<CuentaPorCobrarFilaDto> Reservas { get; set; } = new();
    public List<AlertaCobroDto> Alertas { get; set; } = new();
}
