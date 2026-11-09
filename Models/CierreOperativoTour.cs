using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.Models;

/// <summary>Instantánea del control de salida. No modifica estado comercial ni reservas.</summary>
public sealed class CierreOperativoTour
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public Tour Tour { get; set; } = null!;
    public DateTime FechaCierreUtc { get; set; }
    [Required, MaxLength(100)]
    public string CerradoPor { get; set; } = "";
    public int PlazasConfirmadas { get; set; }
    public int PasajerosPresentes { get; set; }
    public int PasajerosAusentes { get; set; }
    [MaxLength(1000)]
    public string? Incidencias { get; set; }
}
