using System.ComponentModel.DataAnnotations;

namespace TourSmart.Web.Models;

public sealed class EditarPasajerosViewModel
{
    public int Id { get; set; }
    public string Tour { get; set; } = "";
    public int CantidadPersonas { get; set; }
    public string Estado { get; set; } = "";
    public DateTime InicioTour { get; set; }
    public TimeSpan Hora { get; set; }
    public List<string> Pasajeros { get; set; } = new();
}
