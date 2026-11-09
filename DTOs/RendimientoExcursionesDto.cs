namespace AgenciaToursAPI.DTOs;

public sealed class RendimientoExcursionFilaDto
{
    public int TourId { get; set; }
    public string Tour { get; set; } = "";
    public DateTime FechaSalida { get; set; }
    public int Capacidad { get; set; }
    public int Confirmados { get; set; }
    public int Presentes { get; set; }
    public int Ausentes { get; set; }
    public bool TieneIncidencias { get; set; }
    public decimal PorcentajeOcupacion => Capacidad > 0 ? Math.Round(100m * Confirmados / Capacidad, 1) : 0;
    public decimal PorcentajeAsistencia => Confirmados > 0 ? Math.Round(100m * Presentes / Confirmados, 1) : 0;
}

public sealed class RendimientoExcursionesDto
{
    public DateTime DesdeUtc { get; set; }
    public DateTime HastaExclusivoUtc { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; } = 25;
    public int TotalExcursiones { get; set; }
    public int CapacidadTotal { get; set; }
    public int TotalConfirmados { get; set; }
    public int TotalPresentes { get; set; }
    public int TotalAusentes { get; set; }
    public int TotalConIncidencias { get; set; }
    public decimal PorcentajeOcupacion => CapacidadTotal > 0 ? Math.Round(100m * TotalConfirmados / CapacidadTotal, 1) : 0;
    public decimal PorcentajeAsistencia => TotalConfirmados > 0 ? Math.Round(100m * TotalPresentes / TotalConfirmados, 1) : 0;
    public List<RendimientoExcursionFilaDto> Excursiones { get; set; } = new();
}
