namespace AgenciaToursAPI.Security;

/// <summary>Reglas puras usadas por los controladores y las pruebas automatizadas.</summary>
public static class ReglasReserva
{
    public static bool HayCupos(int capacidad, int ocupados, int solicitados) =>
        capacidad > 0 && ocupados >= 0 && solicitados > 0 &&
        (long)ocupados + solicitados <= capacidad;

    // El mínimo del adelanto se redondea hacia arriba al centavo.
    public static decimal AdelantoMinimo(decimal total, int porcentaje)
    {
        if (total < 0) throw new ArgumentOutOfRangeException(nameof(total));
        if (porcentaje is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(porcentaje));
        return Math.Ceiling(total * porcentaje) / 100m;
    }

    public static decimal Saldo(decimal total, decimal pagado, decimal devuelto) =>
        total - (pagado - devuelto);

    public static bool TieneAdelanto(decimal total, int porcentaje,
        decimal pagado, decimal devuelto) =>
        pagado - devuelto >= AdelantoMinimo(total, porcentaje);
}
