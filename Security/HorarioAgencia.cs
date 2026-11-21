namespace AgenciaToursAPI.Security;

/// <summary>
/// Las fechas de salida de TourSmart RD se introducen en la hora civil
/// de República Dominicana. Los instantes de auditoría/vencimiento siguen en UTC.
/// </summary>
public static class HorarioAgencia
{
    private static readonly TimeZoneInfo Zona = CargarZona();

    private static TimeZoneInfo CargarZona()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo"); }
        catch (TimeZoneNotFoundException)
        {
            // Identificador tradicional de Windows en instalaciones sin ICU.
            return TimeZoneInfo.FindSystemTimeZoneById("SA Western Standard Time");
        }
    }

    public static DateTime Ahora =>
        DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona),
            DateTimeKind.Unspecified);
}
