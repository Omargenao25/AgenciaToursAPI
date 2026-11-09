namespace TourSmart.Web.Models;

public sealed class CatalogoViewModel
{
    public IReadOnlyList<TourCard> Tours { get; set; } = Array.Empty<TourCard>();
    public string? Busqueda { get; set; }
    public string? Categoria { get; set; }
    public int? PaisId { get; set; }
    public int? DestinoId { get; set; }
    public bool SoloDisponibles { get; set; }
    public IReadOnlyList<OpcionCatalogo> Paises { get; set; } = Array.Empty<OpcionCatalogo>();
    public IReadOnlyList<DestinoCatalogo> Destinos { get; set; } = Array.Empty<DestinoCatalogo>();
    public IReadOnlyList<string> Categorias { get; set; } = Array.Empty<string>();
    public bool ServicioDisponible { get; set; } = true;
    public string? Mensaje { get; set; }
    public int TotalTours => Tours.Count;
}
