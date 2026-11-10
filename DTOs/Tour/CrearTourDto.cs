using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.DTOs
{
    public class CrearTourDto
    {

        [StringLength(500)]
        [RegularExpression(@"^https://[^\s/?#@:]+(?:\:[0-9]{2,5})?(?:[/\?#][^\s]*)?$", ErrorMessage = "La imagen debe usar una URL HTTPS valida.")]
        public string? ImagenUrl { get; set; }

        [Required(ErrorMessage = "El nombre del tour es obligatorio.")]
        [StringLength(150,MinimumLength = 3,ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
        public string Nombre { get; set; } = string.Empty;


        [Range(1,int.MaxValue, ErrorMessage = "Debe seleccionar un país válido." )]
        public int PaisId { get; set; }


        [Range( 1, int.MaxValue, ErrorMessage = "Debe seleccionar un destino válido.")]
        public int DestinoId { get; set; }


        [Range(1,int.MaxValue,ErrorMessage = "Debe seleccionar una categoría válida.")]
        public int CategoriaId { get; set; }


        [Range(1,int.MaxValue,ErrorMessage = "Debe seleccionar un guía válido.")]
        public int GuiaId { get; set; }


        [Range(1,int.MaxValue, ErrorMessage = "Debe seleccionar un transporte válido."
        )]
        public int TransporteId { get; set; }


        [Required(ErrorMessage = "La fecha del tour es obligatoria.")]
        public DateTime Fecha { get; set; }


        [Required(ErrorMessage = "La hora del tour es obligatoria.")]
        public TimeSpan Hora { get; set; }


        [Range(typeof(decimal),"0.01","10000000", ErrorMessage = "El precio debe ser mayor que cero.")]
        public decimal Precio { get; set; }

        [Range(1, 100000)]
        public int CapacidadMaxima { get; set; }
    }
}