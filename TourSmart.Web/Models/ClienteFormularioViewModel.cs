using System.ComponentModel.DataAnnotations;

namespace TourSmart.Web.Models;

public sealed class ClienteFormularioViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80)]
    public string Apellido { get; set; } = "";

    // Se conserva para deserializar clientes antiguos y mostrar su identidad anterior.
    public string? Cedula { get; set; }

    [Required(ErrorMessage = "Selecciona el tipo de documento.")]
    public string TipoDocumento { get; set; } = "CedulaDominicana";

    [Required(ErrorMessage = "Introduce el número de documento.")]
    [StringLength(40)]
    public string NumeroDocumento { get; set; } = "";

    [Required(ErrorMessage = "Indica el país emisor.")]
    [StringLength(80)]
    public string PaisEmisorDocumento { get; set; } = "República Dominicana";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20)]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Introduce un correo válido.")]
    [StringLength(150)]
    public string Correo { get; set; } = "";
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TipoDocumento is not ("CedulaDominicana" or "Pasaporte" or "Residencia" or "Otro"))
            yield return new ValidationResult("Tipo de documento inválido.", new[] { nameof(TipoDocumento) });

        if (TipoDocumento == "CedulaDominicana")
        {
            if (PaisEmisorDocumento != "República Dominicana")
                yield return new ValidationResult("La cédula dominicana se emite en República Dominicana.", new[] { nameof(PaisEmisorDocumento) });
            if (!System.Text.RegularExpressions.Regex.IsMatch(NumeroDocumento ?? "", @"^[0-9]{11}$"))
                yield return new ValidationResult("La cédula debe tener exactamente 11 dígitos.", new[] { nameof(NumeroDocumento) });
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(NumeroDocumento ?? "", @"^[A-Za-z0-9-]{5,40}$"))
            yield return new ValidationResult("Introduce de 5 a 40 caracteres alfanuméricos o guiones.", new[] { nameof(NumeroDocumento) });
    }
}
