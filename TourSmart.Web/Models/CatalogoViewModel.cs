namespace TourSmart.Web.Models;

public sealed class CatalogoViewModel
{
    public IReadOnlyList<TourCard> Tours { get; set; } = Array.Empty<TourCard>();
    public string? Busqueda { get; set; }
    public string? Categoria { get; set; }
    public bool ServicioDisponible { get; set; } = true;
    public string? Mensaje { get; set; }
    public int TotalTours => Tours.Count;
}
