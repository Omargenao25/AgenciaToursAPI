using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.DTOs;

public sealed class RegistrarReembolsoDto
{
    [Range(typeof(decimal), "0.01", "999999999999.99")]
    public decimal Monto { get; set; }
    [Range(1, int.MaxValue)]
    public int MetodoPagoId { get; set; }
    [Required, MinLength(8), MaxLength(200)]
    public string Motivo { get; set; } = "";
    [MaxLength(100)]
    public string Referencia { get; set; } = "";
    [MaxLength(200)]
    public string Observacion { get; set; } = "";
    public Guid ClaveOperacion { get; set; }
}

public sealed class ReembolsoRespuestaDto
{
    public int Id { get; set; }
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = "";
    public DateTime FechaRegistroUtc { get; set; }
    public string Referencia { get; set; } = "";
    public string Motivo { get; set; } = "";
    public string Observacion { get; set; } = "";
    public string RegistradoPor { get; set; } = "";
}
