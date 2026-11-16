using AgenciaToursAPI.Data;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers;

/// <summary>
/// Indicadores de comprobantes. El importe reportado NO equivale a ingreso,
/// y no se suma a pagos ya verificados de la contabilidad financiera.
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Administrador)]
[Route("api/reportes/comprobantes")]
public sealed class ReporteComprobantesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obtener([FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta, CancellationToken ct)
    {
        var hoy = DateTime.UtcNow.Date;
        var inicio = (desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
        var fin = (hasta ?? hoy).Date;
        if (inicio.Year < 2000 || fin < inicio || fin >= DateTime.MaxValue.Date ||
            (fin - inicio).TotalDays > 366)
            return BadRequest("Selecciona un período UTC válido de hasta 366 días.");

        var exclusivo = fin.AddDays(1);
        // Los totales corresponden a la fecha en que se reportó el comprobante,
        // incluso si después se aprobó o rechazó.
        var baseQuery = db.ComprobantesTransferencia.AsNoTracking()
            .Where(c => c.CreadoUtc >= inicio && c.CreadoUtc < exclusivo);

        var porEstado = await baseQuery.GroupBy(c => c.Estado)
            .Select(g => new {
                Estado = g.Key,
                Cantidad = g.Count(),
                Monto = g.Sum(c => c.MontoReportado)
            }).ToListAsync(ct);

        var aprobados = porEstado.FirstOrDefault(x => x.Estado == "Aprobado");
        var rechazados = porEstado.FirstOrDefault(x => x.Estado == "Rechazado");
        var pendientes = porEstado.FirstOrDefault(x => x.Estado == "Pendiente");

        // KPI operativo global no restringido por fechas de reporte:
        // los casos aún pendientes en este momento requieren intervención.
        var totalPendientesActuales = await db.ComprobantesTransferencia.AsNoTracking()
            .CountAsync(c => c.Estado == "Pendiente", ct);
        var pendientesVencidos = await db.ComprobantesTransferencia.AsNoTracking()
            .CountAsync(c => c.Estado == "Pendiente" &&
                c.Reserva.Estado == EstadosReserva.Pendiente &&
                c.Reserva.VencePendienteUtc != null &&
                c.Reserva.VencePendienteUtc <= DateTime.UtcNow, ct);

        var recientes = await baseQuery
            .OrderByDescending(c => c.CreadoUtc).ThenByDescending(c => c.Id)
            .Take(75)
            .Select(c => new
            {
                c.Id, c.ReservaId, c.CreadoUtc, c.MontoReportado, c.Referencia,
                c.Estado, c.MotivoRevision, c.RevisadoUtc, c.PagoReservaId,
                Cliente = c.Cliente.Nombre + " " + c.Cliente.Apellido,
                Tour = c.Reserva.Tour != null ? c.Reserva.Tour.Nombre : ""
            }).ToListAsync(ct);

        var porExcursion = await baseQuery
            .GroupBy(c => new { c.Reserva.TourId, Nombre = c.Reserva.Tour!.Nombre })
            .Select(g => new
            {
                Id = g.Key.TourId, Nombre = g.Key.Nombre,
                Total = g.Count(),
                Pendientes = g.Count(c => c.Estado == "Pendiente"),
                Aprobados = g.Count(c => c.Estado == "Aprobado"),
                Rechazados = g.Count(c => c.Estado == "Rechazado"),
                MontoAprobado = g.Sum(c => c.Estado == "Aprobado" ? c.MontoReportado : 0m)
            }).OrderByDescending(x => x.Total).ThenBy(x => x.Nombre)
            .Take(50).ToListAsync(ct);

        Response.Headers.CacheControl = "private, no-store";
        return Ok(new
        {
            DesdeUtc = inicio,
            HastaExclusivoUtc = exclusivo,
            TotalComprobantes = porEstado.Sum(x => x.Cantidad),
            CantidadPendientes = pendientes?.Cantidad ?? 0,
            CantidadAprobados = aprobados?.Cantidad ?? 0,
            CantidadRechazados = rechazados?.Cantidad ?? 0,
            MontoPendienteReportado = pendientes?.Monto ?? 0m,
            MontoAprobadoReportado = aprobados?.Monto ?? 0m,
            MontoRechazadoReportado = rechazados?.Monto ?? 0m,
            PendientesActualesGlobal = totalPendientesActuales,
            PendientesConPlazoOriginalVencido = pendientesVencidos,
            PorExcursion = porExcursion,
            Recientes = recientes
        });
    }
}
