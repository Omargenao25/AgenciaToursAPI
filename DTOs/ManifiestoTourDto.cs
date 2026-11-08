namespace AgenciaToursAPI.DTOs;

/// <summary>
/// Lista operativa de titulares de reservas. NO es un registro individual
/// de acompañantes: solo existe la cantidad de personas por reserva.
/// </summary>
public sealed class ManifiestoReservaDto
{
    public int ReservaId { get; set; }
    public string Titular { get; set; } = "";
    public string Telefono { get; set; } = "";
    public int CantidadPersonas { get; set; }
    public string Estado { get; set; } = "";
    public decimal Total { get; set; }
    public decimal NetoRecibido { get; set; }
    public decimal Saldo => Math.Max(0m, Total - NetoRecibido);
}

public sealed class ManifiestoTourDto
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
    public int CuposComprometidos => CuposConfirmados + CuposPendientes;
    public int CuposDisponibles => Math.Max(0, CapacidadMaxima - CuposComprometidos);
    public List<ManifiestoReservaDto> Confirmadas { get; set; } = new();
    public List<ManifiestoReservaDto> Pendientes { get; set; } = new();
}
