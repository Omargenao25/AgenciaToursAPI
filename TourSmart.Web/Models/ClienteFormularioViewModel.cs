using System.ComponentModel.DataAnnotations;

namespace TourSmart.Web.Models;

public sealed class ClienteFormularioViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80)]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "La cédula es obligatoria.")]
    [StringLength(20)]
    public string Cedula { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20)]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Introduce un correo válido.")]
    [StringLength(150)]
    public string Correo { get; set; } = "";
}
