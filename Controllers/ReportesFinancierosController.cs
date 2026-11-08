using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers;

[ApiController]
[Authorize(Roles = Roles.Administrador)]
[Route("api/reportes/finanzas")]
public sealed class ReportesFinancierosController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Resumen([FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta, CancellationToken ct)
    {
        // Fechas calendario UTC, ambos extremos incluidos para el usuario.
        var hoy = DateTime.UtcNow.Date;
        var inicio = (desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
        var fin = (hasta ?? hoy).Date;
        if (inicio.Year < 2000 || fin < inicio || fin >= DateTime.MaxValue.Date ||
            (fin - inicio).TotalDays > 366)
            return BadRequest("Selecciona un período válido de hasta 366 días, a partir del año 2000.");

        var finExclusivo = fin.AddDays(1);
        var pagos = db.PagosReserva.AsNoTracking()
            .Where(p => p.FechaRegistroUtc >= inicio && p.FechaRegistroUtc < finExclusivo);
        var reembolsos = db.ReembolsosReserva.AsNoTracking()
            .Where(p => p.FechaRegistroUtc >= inicio && p.FechaRegistroUtc < finExclusivo);

        // Los KPI no se derivan de la lista, que puede estar truncada.
        var totalCobrado = await pagos.SumAsync(p => (decimal?)p.Monto, ct) ?? 0m;
        var totalDevuelto = await reembolsos.SumAsync(p => (decimal?)p.Monto, ct) ?? 0m;
        var cantidadPagos = await pagos.CountAsync(ct);
        var cantidadReembolsos = await reembolsos.CountAsync(ct);

        var movimientosPagos = await pagos.OrderByDescending(p => p.FechaRegistroUtc)
            .ThenByDescending(p => p.Id).Take(500)
            .Select(p => new MovimientoFinancieroDto {
                Id = p.Id, ReservaId = p.ReservaId, Tipo = "Pago",
                Monto = p.Monto, FechaRegistroUtc = p.FechaRegistroUtc,
                MetodoPago = p.MetodoPago.Nombre, Referencia = p.Referencia,
                RegistradoPor = p.RegistradoPor,
                Tour = p.Reserva.Tour != null ? p.Reserva.Tour.Nombre : "",
                Cliente = p.Reserva.Cliente != null
                    ? p.Reserva.Cliente.Nombre + " " + p.Reserva.Cliente.Apellido : ""
            }).ToListAsync(ct);

        var movimientosReembolsos = await reembolsos.OrderByDescending(p => p.FechaRegistroUtc)
            .ThenByDescending(p => p.Id).Take(500)
            .Select(p => new MovimientoFinancieroDto {
                Id = p.Id, ReservaId = p.ReservaId, Tipo = "Reembolso",
                Monto = p.Monto, FechaRegistroUtc = p.FechaRegistroUtc,
                MetodoPago = p.MetodoPago.Nombre, Referencia = p.Referencia,
                RegistradoPor = p.RegistradoPor,
                Tour = p.Reserva.Tour != null ? p.Reserva.Tour.Nombre : "",
                Cliente = p.Reserva.Cliente != null
                    ? p.Reserva.Cliente.Nombre + " " + p.Reserva.Cliente.Apellido : ""
            }).ToListAsync(ct);

        var reporte = new ReporteFinancieroDto {
            DesdeUtc = inicio, HastaExclusivoUtc = finExclusivo,
            TotalCobrado = totalCobrado, TotalDevuelto = totalDevuelto,
            CantidadPagos = cantidadPagos, CantidadReembolsos = cantidadReembolsos,
            Movimientos = movimientosPagos.Concat(movimientosReembolsos)
                .OrderByDescending(x => x.FechaRegistroUtc)
                .ThenByDescending(x => x.Tipo).ThenByDescending(x => x.Id)
                .Take(500).ToList()
        };
        Response.Headers.CacheControl = "private, no-store";
        return Ok(reporte);
    }
}
