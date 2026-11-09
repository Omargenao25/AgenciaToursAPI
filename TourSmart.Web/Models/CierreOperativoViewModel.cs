using System.ComponentModel.DataAnnotations;

namespace TourSmart.Web.Models;

public sealed class CierreOperativoDetalle
{
    public int TourId { get; set; }
    public DateTime FechaCierreUtc { get; set; }
    public string CerradoPor { get; set; } = "";
    public int PlazasConfirmadas { get; set; }
    public int PasajerosPresentes { get; set; }
    public int PasajerosAusentes { get; set; }
    public string? Incidencias { get; set; }
}

public sealed class EstadoCierreOperativo
{
    public bool Cerrado { get; set; }
    public CierreOperativoDetalle? Cierre { get; set; }
}

public sealed class CierreOperativoViewModel
{
    public ManifiestoTourViewModel Manifiesto { get; set; } = new();
    public EstadoCierreOperativo Estado { get; set; } = new();

    [StringLength(1000, ErrorMessage = "Máximo 1000 caracteres.")]
    public string? Incidencias { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Confirma que revisaste el manifiesto antes de cerrar.")]
    public bool Confirmar { get; set; }
}
