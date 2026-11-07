using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AgenciaToursAPI.Models
{
    public class Cliente : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
        [MaxLength(80,ErrorMessage = "El nombre del cliente no puede superar los 80 caracteres.")]
        public string Nombre { get; set; } = string.Empty;


        [Required(ErrorMessage = "El apellido del cliente es obligatorio.")]
        [MaxLength( 80, ErrorMessage = "El apellido del cliente no puede superar los 80 caracteres.")]
        public string Apellido { get; set; } = string.Empty;


        // Campo legado: conservado para preservar compatibilidad con registros dominicanos.
        [MaxLength(20)]
        public string? Cedula { get; set; }

        [Required]
        [MaxLength(30)]
        public string TipoDocumento { get; set; } = "CedulaDominicana";

        [Required]
        [MaxLength(40)]
        public string NumeroDocumento { get; set; } = string.Empty;

        [Required]
        [MaxLength(80)]
        public string PaisEmisorDocumento { get; set; } = "República Dominicana";

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [MaxLength(20,ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
        public string Telefono { get; set; } = string.Empty;


        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [MaxLength(150,ErrorMessage = "El correo no puede superar los 150 caracteres.")]
        public string Correo { get; set; } = string.Empty;


        [JsonIgnore]
        public List<Reserva> Reservas { get; set; } = new();
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TipoDocumento is not ("CedulaDominicana" or "Pasaporte" or "Residencia" or "Otro"))
                yield return new ValidationResult("Tipo de documento no válido.", new[] { nameof(TipoDocumento) });

            if (string.IsNullOrWhiteSpace(PaisEmisorDocumento))
                yield return new ValidationResult("Indica el país emisor.", new[] { nameof(PaisEmisorDocumento) });

            if (TipoDocumento == "CedulaDominicana")
            {
                if (PaisEmisorDocumento != "República Dominicana")
                    yield return new ValidationResult("La cédula dominicana debe indicar República Dominicana como país emisor.", new[] { nameof(PaisEmisorDocumento) });
                if (!System.Text.RegularExpressions.Regex.IsMatch(NumeroDocumento ?? "", @"^[0-9]{11}$"))
                    yield return new ValidationResult("La cédula dominicana debe tener exactamente 11 dígitos.", new[] { nameof(NumeroDocumento) });
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(NumeroDocumento ?? "", @"^[A-Za-z0-9-]{5,40}$"))
                yield return new ValidationResult("El documento debe tener entre 5 y 40 caracteres alfanuméricos o guiones.", new[] { nameof(NumeroDocumento) });
        }
    }
}