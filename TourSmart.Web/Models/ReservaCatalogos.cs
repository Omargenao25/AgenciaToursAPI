namespace TourSmart.Web.Models;

public sealed class ClienteReserva
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
}
public sealed class MetodoPagoReserva
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}
