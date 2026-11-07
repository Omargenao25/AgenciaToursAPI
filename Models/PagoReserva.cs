using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenciaToursAPI.Models;

public sealed class PagoReserva
{
    public int Id { get; set; }
    public int ReservaId { get; set; }
    public Reserva Reserva { get; set; } = null!;
    public int MetodoPagoId { get; set; }
    public MetodoPago MetodoPago { get; set; } = null!;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }
    public DateTime FechaRegistroUtc { get; set; }
    [MaxLength(100)]
    public string Referencia { get; set; } = "";
    [MaxLength(200)]
    public string Observacion { get; set; } = "";
    public int RegistradoPorUsuarioId { get; set; }
    [MaxLength(100)]
    public string RegistradoPor { get; set; } = "";
    public Guid ClaveOperacion { get; set; }
}
