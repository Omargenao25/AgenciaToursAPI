namespace TourSmart.Web.Models;

public sealed class ComprobanteReservaViewModel
{
    public int Id { get; set; }
    public string Cliente { get; set; } = "";
    public string Tour { get; set; } = "";
    public int TourId { get; set; }
    public DateTime? FechaTour { get; set; }
    public TimeSpan? HoraTour { get; set; }
    public string MetodoPago { get; set; } = "";
    public DateTime FechaReserva { get; set; }
    public int CantidadPersonas { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "";
    public DateTime? FechaCambioEstado { get; set; }
}
