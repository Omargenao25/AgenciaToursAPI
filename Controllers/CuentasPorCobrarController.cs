using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers;

[ApiController]
[Authorize(Roles = Roles.Administrador)]
[Route("api/reportes/cuentas-por-cobrar")]
public sealed class CuentasPorCobrarController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        // Hora de inicio de los tours almacenada como fecha y hora locales de la agencia.
        var ahoraLocal = DateTime.Now;
        var ahoraUtc = DateTime.UtcNow;

        // Incluye únicamente reservas activas de tours por comenzar. No confundir
        // solicitudes pendientes con ingresos confirmados ni sumar canceladas.
        var items = await db.Reservas.AsNoTracking()
            .Where(r => r.Estado != "Cancelada" && r.Tour != null &&
                (r.Tour.Fecha > ahoraLocal.Date ||
                 (r.Tour.Fecha == ahoraLocal.Date && r.Tour.Hora > ahoraLocal.TimeOfDay)))
            .Select(r => new
            {
                ReservaId = r.Id,
                r.TourId,
                Tour = r.Tour!.Nombre,
                Cliente = r.Cliente != null ? r.Cliente.Nombre + " " + r.Cliente.Apellido : "",
                Estado = r.Estado,
                FechaTour = r.Tour.Fecha,
                HoraTour = r.Tour.Hora,
                r.VencePendienteUtc,
                r.Total,
                Porcentaje = r.Tour.PorcentajeAdelanto,
                Pagos = db.PagosReserva.Where(p => p.ReservaId == r.Id)
                    .Sum(p => (decimal?)p.Monto) ?? 0m,
                Reembolsos = db.ReembolsosReserva.Where(p => p.ReservaId == r.Id)
                    .Sum(p => (decimal?)p.Monto) ?? 0m
            })
            .OrderBy(x => x.FechaTour).ThenBy(x => x.HoraTour).ThenBy(x => x.ReservaId)
            .ToListAsync(ct);

        var filas = items.Select(x =>
        {
            var neto = x.Pagos - x.Reembolsos;
            var minimo = Math.Ceiling(x.Total * x.Porcentaje) / 100m;
            var faltaAdelanto = Math.Max(0m, minimo - neto);
            var situacion = x.Estado switch
            {
                "Confirmada" when x.Total > neto => "Confirmada · saldo pendiente",
                "Confirmada" => "Confirmada · pagada",
                "Pendiente" when x.VencePendienteUtc.HasValue &&
                    x.VencePendienteUtc <= ahoraUtc && x.Pagos > 0m =>
                    "Revisión manual · plazo vencido con pagos",
                "Pendiente" when x.VencePendienteUtc.HasValue &&
                    x.VencePendienteUtc <= ahoraUtc =>
                    "Vencimiento sin pagos · pendiente de procesar",
                "Pendiente" when faltaAdelanto > 0 => "Falta adelanto para confirmar",
                "Pendiente" => "Adelanto cubierto · confirmar",
                _ => "Revisar"
            };
            return new CuentaPorCobrarFilaDto
            {
                ReservaId = x.ReservaId, TourId = x.TourId, Tour = x.Tour,
                Cliente = x.Cliente, EstadoReserva = x.Estado,
                InicioTour = x.FechaTour.Date.Add(x.HoraTour),
                VenceAdelantoUtc = x.VencePendienteUtc,
                Total = x.Total, Pagos = x.Pagos, Reembolsos = x.Reembolsos,
                AdelantoMinimo = minimo, Situacion = situacion
            };
        }).ToList();

        var pendientes = filas.Where(x => x.EstadoReserva == "Pendiente").ToList();
        var confirmadas = filas.Where(x => x.EstadoReserva == "Confirmada").ToList();
        var reporte = new CuentasPorCobrarDto
        {
            ConsultadoUtc = ahoraUtc,
            TotalReservasActivas = filas.Count,
            ReservasConfirmadas = confirmadas.Count,
            ReservasPendientes = pendientes.Count,
            TotalSaldoConfirmado = confirmadas.Sum(x => x.Saldo),
            TotalSaldoPendienteSinConfirmar = pendientes.Sum(x => x.Saldo),
            RequierenAtencion = pendientes.Count(x =>
                x.Situacion.StartsWith("Revisión manual") ||
                x.Situacion.StartsWith("Vencimiento sin pagos")),
            Reservas = filas
        };
        Response.Headers.CacheControl = "private, no-store";
        return Ok(reporte);
    }
}
