using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.Models;

public sealed class PasajeroReserva
{
    public int Id { get; set; }
    public int ReservaId { get; set; }
    public Reserva Reserva { get; set; } = null!;
    public int Orden { get; set; }
    [Required, MaxLength(120)]
    public string NombreCompleto { get; set; } = "";
}
