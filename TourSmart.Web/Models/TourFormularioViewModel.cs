using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TourSmart.Web.Models;

public sealed class TourFormularioViewModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Introduce el nombre.")]
    [StringLength(150, MinimumLength = 3)]
    public string Nombre { get; set; } = "";
    [StringLength(500)]
    [RegularExpression(@"^(?:https://[^\s/?#@:]+(?:\:[0-9]{2,5})?(?:[/\?#][^\s]*)?|/uploads/tours/[a-f0-9]{32}\.(?:jpg|png|webp))$", ErrorMessage = "Usa HTTPS o selecciona una foto desde tu equipo.")]
    public string? ImagenUrl { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un país.")]
    public int PaisId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un destino.")]
    public int DestinoId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría.")]
    public int CategoriaId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un guía.")]
    public int GuiaId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un transporte.")]
    public int TransporteId { get; set; }
    [Required]
    [DataType(DataType.Date)]
    public DateTime Fecha { get; set; } = DateTime.Today.AddDays(7);
    [Required]
    public TimeSpan Hora { get; set; } = new(9, 0, 0);
    [Range(typeof(decimal), "0.01", "10000000", ErrorMessage = "El precio debe ser mayor que cero.")]
    public decimal Precio { get; set; }
    [Range(1, 100000, ErrorMessage = "La capacidad debe ser mayor que cero.")]
    public int CapacidadMaxima { get; set; } = 20;
    public int CuposReservados { get; set; }
    public bool TieneReservas => CuposReservados > 0;
    public IReadOnlyList<SelectListItem> Paises { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Destinos { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Categorias { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Guias { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Transportes { get; set; } = Array.Empty<SelectListItem>();
}
