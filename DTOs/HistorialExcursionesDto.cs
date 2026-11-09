namespace AgenciaToursAPI.DTOs;

public sealed class HistorialExcursionFilaDto
{
    public int TourId { get; set; }
    public string Tour { get; set; } = "";
    public DateTime FechaSalida { get; set; }
    public TimeSpan HoraSalida { get; set; }
    public string Guia { get; set; } = "";
    public string Transporte { get; set; } = "";
    public DateTime FechaCierreUtc { get; set; }
    public string CerradoPor { get; set; } = "";
    public int PlazasConfirmadas { get; set; }
    public int PasajerosPresentes { get; set; }
    public int PasajerosAusentes { get; set; }
    public string? Incidencias { get; set; }
    public bool TieneIncidencias => !string.IsNullOrWhiteSpace(Incidencias);
}

public sealed class HistorialExcursionesDto
{
    public DateTime DesdeUtc { get; set; }
    public DateTime HastaExclusivoUtc { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalCierres { get; set; }
    public int TotalPlazasConfirmadas { get; set; }
    public int TotalPresentes { get; set; }
    public int TotalAusentes { get; set; }
    public int TotalConIncidencias { get; set; }
    public List<HistorialExcursionFilaDto> Cierres { get; set; } = new();
}
