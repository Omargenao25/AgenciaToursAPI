namespace TourSmart.Web.Models;

public sealed class ClientesListadoViewModel
{
    public string Busqueda { get; set; } = "";
    public int Pagina { get; set; } = 1;
    public int TamanoPagina => 10;
    public int Total { get; set; }
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling((double)Total / TamanoPagina));
    public bool ApiDisponible { get; set; } = true;
    public string? Error { get; set; }
    public IReadOnlyList<ClienteFormularioViewModel> Clientes { get; set; } =
        Array.Empty<ClienteFormularioViewModel>();
}
