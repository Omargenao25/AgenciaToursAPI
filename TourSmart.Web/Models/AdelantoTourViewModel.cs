using System.ComponentModel.DataAnnotations;

namespace TourSmart.Web.Models;

public sealed class AdelantoTourViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    [Range(0, 100, ErrorMessage = "El porcentaje debe estar entre 0 y 100.")]
    public int PorcentajeAdelanto { get; set; } = 30;
    public bool TieneReservas { get; set; }
}
