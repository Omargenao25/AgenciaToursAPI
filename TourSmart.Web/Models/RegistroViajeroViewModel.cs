using System.ComponentModel.DataAnnotations;

namespace TourSmart.Web.Models;

public sealed class RegistroViajeroViewModel : IValidatableObject
{
    [Required, StringLength(80)] public string Nombre { get; set; } = "";
    [Required, StringLength(80)] public string Apellido { get; set; } = "";
    [Required, EmailAddress, StringLength(150)] public string Correo { get; set; } = "";
    [Required, StringLength(20)] public string Telefono { get; set; } = "";
    [Required] public string TipoDocumento { get; set; } = "CedulaDominicana";
    [Required, StringLength(40)] public string NumeroDocumento { get; set; } = "";
    [Required, StringLength(80)] public string PaisEmisorDocumento { get; set; } = "República Dominicana";
    [Required, StringLength(100, MinimumLength = 8)] [DataType(DataType.Password)]
    public string Contrasena { get; set; } = "";
    [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmarContrasena { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (TipoDocumento is not ("CedulaDominicana" or "Pasaporte" or "Residencia" or "Otro"))
            yield return new ValidationResult("Selecciona un documento válido.", new[] { nameof(TipoDocumento) });
        if (TipoDocumento == "CedulaDominicana")
        {
            if (PaisEmisorDocumento != "República Dominicana")
                yield return new ValidationResult("El país emisor debe ser República Dominicana.", new[] { nameof(PaisEmisorDocumento) });
            if (!System.Text.RegularExpressions.Regex.IsMatch(NumeroDocumento ?? "", @"^[0-9]{11}$"))
                yield return new ValidationResult("La cédula debe tener 11 dígitos.", new[] { nameof(NumeroDocumento) });
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(NumeroDocumento ?? "", @"^[A-Za-z0-9-]{5,40}$"))
            yield return new ValidationResult("Número de documento inválido.", new[] { nameof(NumeroDocumento) });
    }
}
