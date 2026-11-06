namespace TourSmart.Web.Models;

public sealed class DashboardViewModel
{
    public string Rol { get; set; } = "";
    public bool ApiDisponible { get; set; } = true;
    public string? Error { get; set; }
    public IReadOnlyList<TourCard> Tours { get; set; } = Array.Empty<TourCard>();
    public IReadOnlyList<ReservaPanel> Reservas { get; set; } = Array.Empty<ReservaPanel>();
    public int ToursFuturos => Tours.Count(t => t.Estado == "Disponible");
    public int Pendientes => Reservas.Count(r => r.Estado == "Pendiente");
    public int Confirmadas => Reservas.Count(r => r.Estado == "Confirmada");
    public int Canceladas => Reservas.Count(r => r.Estado == "Cancelada");
    public int CuposDisponibles => Tours.Where(t => t.Estado == "Disponible").Sum(t => t.CuposDisponibles);
}
