using System.ComponentModel.DataAnnotations;

namespace TourSmart.Web.Models;

public sealed class MiPerfilViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80)]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20)]
    public string Telefono { get; set; } = "";

    // Solo lectura: no se envían al actualizar datos personales.
    public string Correo { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string NumeroDocumento { get; set; } = "";
    public string PaisEmisorDocumento { get; set; } = "";
}
