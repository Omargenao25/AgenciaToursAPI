using AgenciaToursAPI.Models;

namespace AgenciaToursAPI.DTOs;

public static class ValidacionPasajeros
{
    public static bool IntentarNormalizar(
        int cantidadPersonas, IList<string>? nombres,
        out List<PasajeroReserva> pasajeros, out string error)
    {
        pasajeros = new();
        error = "";
        if (cantidadPersonas < 1 || cantidadPersonas > 100 ||
            nombres is null || nombres.Count != cantidadPersonas)
        {
            error = "Debes registrar exactamente un nombre por cada plaza de la reserva.";
            return false;
        }
        for (var i = 0; i < nombres.Count; i++)
        {
            var nombre = string.Join(" ", (nombres[i] ?? "").Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
            if (nombre.Length < 3 || nombre.Length > 120 || !nombre.Any(char.IsLetter) ||
                nombre.Any(char.IsControl))
            {
                error = $"El nombre completo del pasajero {i + 1} no es válido (3 a 120 caracteres).";
                return false;
            }
            pasajeros.Add(new PasajeroReserva { Orden = i + 1, NombreCompleto = nombre });
        }
        return true;
    }
}
