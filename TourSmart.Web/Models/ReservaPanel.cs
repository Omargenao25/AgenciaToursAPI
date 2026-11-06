namespace TourSmart.Web.Models;

public sealed class ReservaPanel
{
    public int Id { get; set; }
    public string Cliente { get; set; } = "";
    public string Tour { get; set; } = "";
    public DateTime FechaReserva { get; set; }
    public int CantidadPersonas { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "";
}
