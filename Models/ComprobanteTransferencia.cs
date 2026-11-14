using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenciaToursAPI.Models;

// Declaración del cliente: NO equivale a un pago bancario recibido.
public sealed class ComprobanteTransferencia
{
    public long Id { get; set; }
    public int ReservaId { get; set; }
    public Reserva Reserva { get; set; } = null!;
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoReportado { get; set; }
    [Required, MaxLength(100)]
    public string Referencia { get; set; } = "";
    [Required, MaxLength(20)]
    public string Estado { get; set; } = "Pendiente";
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevisadoUtc { get; set; }
    public int? RevisadoPorUsuarioId { get; set; }
    [MaxLength(300)]
    public string? MotivoRevision { get; set; }
    public int? PagoReservaId { get; set; }
    public PagoReserva? PagoReserva { get; set; }
    [Required, MaxLength(50)]
    public string ArchivoMime { get; set; } = "";
    [Required]
    public byte[] Archivo { get; set; } = Array.Empty<byte>();
}
