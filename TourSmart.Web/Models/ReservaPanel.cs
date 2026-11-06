namespace TourSmart.Web.Models;

public sealed class ReservaPanel
{
    public int Id { get; set; }
    public string Cliente { get; set; } = "";
    public string Tour { get; set; } = "";
    public int TourId { get; set; }
    public DateTime? FechaTour { get; set; }
    public TimeSpan? HoraTour { get; set; }
    public bool TourPorIniciar => FechaTour.HasValue && HoraTour.HasValue &&
        DateTime.Now < FechaTour.Value.Date.Add(HoraTour.Value);
    public bool PuedeConfirmar => Estado == "Pendiente" && TourPorIniciar;
    public bool PuedeCancelar => (Estado is "Pendiente" or "Confirmada") && TourPorIniciar;
    public DateTime FechaReserva { get; set; }
    public int CantidadPersonas { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "";
}
