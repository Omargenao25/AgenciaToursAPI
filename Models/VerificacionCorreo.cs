using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.Models;

public sealed class VerificacionCorreo
{
    public long Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    [Required, MaxLength(64)]
    public string TokenHash { get; set; } = "";
    public DateTime CreadoUtc { get; set; }
    public DateTime ExpiraUtc { get; set; }
    public DateTime? ConsumidoUtc { get; set; }
}
