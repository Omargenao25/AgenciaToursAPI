namespace TourSmart.Web.Models;

public sealed class RendimientoExcursionFila
{
    public int TourId { get; set; }
    public string Tour { get; set; } = "";
    public DateTime FechaSalida { get; set; }
    public int Capacidad { get; set; }
    public int Confirmados { get; set; }
    public int Presentes { get; set; }
    public int Ausentes { get; set; }
    public bool TieneIncidencias { get; set; }
    public decimal PorcentajeOcupacion { get; set; }
    public decimal PorcentajeAsistencia { get; set; }
}

public sealed class RendimientoExcursionesViewModel
{
    public DateTime DesdeUtc { get; set; }
    public DateTime HastaExclusivoUtc { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
    public int TotalExcursiones { get; set; }
    public int CapacidadTotal { get; set; }
    public int TotalConfirmados { get; set; }
    public int TotalPresentes { get; set; }
    public int TotalAusentes { get; set; }
    public int TotalConIncidencias { get; set; }
    public decimal PorcentajeOcupacion { get; set; }
    public decimal PorcentajeAsistencia { get; set; }
    public List<RendimientoExcursionFila> Excursiones { get; set; } = new();
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalExcursiones / (double)Math.Max(1, TamanoPagina)));
}
