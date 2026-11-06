namespace TourSmart.Web.Models;

public sealed class ReservasListadoViewModel
{
    public string Busqueda { get; set; } = "";
    public string Estado { get; set; } = "Todas";
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; } = 10;
    public int Total { get; set; }
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling((double)Total / TamanoPagina));
    public bool ApiDisponible { get; set; } = true;
    public string? Error { get; set; }
    public IReadOnlyList<ReservaPanel> Reservas { get; set; } = Array.Empty<ReservaPanel>();
}
