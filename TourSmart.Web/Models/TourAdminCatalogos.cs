namespace TourSmart.Web.Models;

public sealed class TourAdminCatalogos
{
    public IReadOnlyList<OpcionCatalogo> Paises { get; set; } = Array.Empty<OpcionCatalogo>();
    public IReadOnlyList<DestinoCatalogo> Destinos { get; set; } = Array.Empty<DestinoCatalogo>();
    public IReadOnlyList<OpcionCatalogo> Categorias { get; set; } = Array.Empty<OpcionCatalogo>();
    public IReadOnlyList<OpcionCatalogo> Guias { get; set; } = Array.Empty<OpcionCatalogo>();
    public IReadOnlyList<TransporteCatalogo> Transportes { get; set; } = Array.Empty<TransporteCatalogo>();
}
public sealed class OpcionCatalogo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}
public sealed class DestinoCatalogo
{
    public int Id { get; set; }
    public int PaisId { get; set; }
    public string Nombre { get; set; } = "";
}
public sealed class TransporteCatalogo
{
    public int Id { get; set; }
    public string Tipo { get; set; } = "";
}
