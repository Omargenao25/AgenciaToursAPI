namespace TourSmart.Web.Services;

/// <summary>
/// Hora civil usada para fechas de excursiones, independientemente de
/// la zona horaria configurada en el servidor Web.
/// </summary>
public static class HorarioAgencia
{
    private static readonly TimeZoneInfo Zona = CargarZona();

    private static TimeZoneInfo CargarZona()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo"); }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SA Western Standard Time");
        }
    }

    public static DateTime Ahora =>
        DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona),
            DateTimeKind.Unspecified);
}
