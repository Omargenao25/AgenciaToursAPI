namespace TourSmart.Web.Models;

public sealed class PasajeroAsistencia
{
    public int Id { get; set; }
    public int Orden { get; set; }
    public string NombreCompleto { get; set; } = "";
    public DateTime? LlegadaUtc { get; set; }
    public string? RegistradoPor { get; set; }
}

public sealed class ManifiestoReserva
{
    public int ReservaId { get; set; }
    public string Titular { get; set; } = "";
    public List<string> Pasajeros { get; set; } = new();
    public string Telefono { get; set; } = "";
    public int CantidadPersonas { get; set; }
    public string Estado { get; set; } = "";
    public decimal Total { get; set; }
    public decimal NetoRecibido { get; set; }
    public decimal Saldo { get; set; }
}

public sealed class ManifiestoTourViewModel
{
    public int TourId { get; set; }
    public string Tour { get; set; } = "";
    public DateTime Fecha { get; set; }
    public TimeSpan Hora { get; set; }
    public string Guia { get; set; } = "";
    public string Transporte { get; set; } = "";
    public int CapacidadMaxima { get; set; }
    public int CuposConfirmados { get; set; }
    public int CuposPendientes { get; set; }
    public int CuposComprometidos { get; set; }
    public int CuposDisponibles { get; set; }
    public List<ManifiestoReserva> Confirmadas { get; set; } = new();
    public List<ManifiestoReserva> Pendientes { get; set; } = new();
}
