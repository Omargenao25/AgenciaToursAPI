using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace AgenciaToursAPI.Controllers;

[ApiController]
[Authorize(Roles = Roles.Administrador)]
[Route("api/reportes/finanzas")]
public sealed class ReportesFinancierosController(ApplicationDbContext db) : ControllerBase
{

    [HttpGet("exportar-csv")]
    public async Task<IActionResult> ExportarCsv([FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta, CancellationToken ct)
    {
        var hoy = DateTime.UtcNow.Date;
        var inicio = (desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
        var fin = (hasta ?? hoy).Date;
        if (inicio.Year < 2000 || fin < inicio || fin >= DateTime.MaxValue.Date ||
            (fin - inicio).TotalDays > 366)
            return BadRequest("Selecciona un período válido de hasta 366 días, a partir del año 2000.");

        var finExclusivo = fin.AddDays(1);
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition =
            $"attachment; filename=\"TourSmart_Finanzas_{inicio:yyyyMMdd}_{fin:yyyyMMdd}.csv\"";

        // Escribir directamente al flujo HTTP evita el límite visual de 500
        // y no acumula en memoria la información financiera.
        await using var writer = new StreamWriter(
            Response.Body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            bufferSize: 16 * 1024, leaveOpen: true);
        await writer.WriteLineAsync("Tipo;FechaUTC;ReservaId;MovimientoId;Cliente;Tour;Metodo;Referencia;Responsable;MontoNetoRD");

        var pagos = db.PagosReserva.AsNoTracking()
            .Where(p => p.FechaRegistroUtc >= inicio && p.FechaRegistroUtc < finExclusivo)
            .OrderBy(p => p.FechaRegistroUtc).ThenBy(p => p.Id)
            .Select(p => new MovimientoFinancieroDto {
                Id = p.Id, ReservaId = p.ReservaId, Tipo = "Pago",
                Monto = p.Monto, FechaRegistroUtc = p.FechaRegistroUtc,
                MetodoPago = p.MetodoPago.Nombre, Referencia = p.Referencia,
                RegistradoPor = p.RegistradoPor,
                Tour = p.Reserva.Tour != null ? p.Reserva.Tour.Nombre : "",
                Cliente = p.Reserva.Cliente != null
                    ? p.Reserva.Cliente.Nombre + " " + p.Reserva.Cliente.Apellido : ""
            });

        await foreach (var pago in pagos.AsAsyncEnumerable().WithCancellation(ct))
            await EscribirMovimiento(writer, pago, ct);

        var devoluciones = db.ReembolsosReserva.AsNoTracking()
            .Where(p => p.FechaRegistroUtc >= inicio && p.FechaRegistroUtc < finExclusivo)
            .OrderBy(p => p.FechaRegistroUtc).ThenBy(p => p.Id)
            .Select(p => new MovimientoFinancieroDto {
                Id = p.Id, ReservaId = p.ReservaId, Tipo = "Reembolso",
                Monto = p.Monto, FechaRegistroUtc = p.FechaRegistroUtc,
                MetodoPago = p.MetodoPago.Nombre, Referencia = p.Referencia,
                RegistradoPor = p.RegistradoPor,
                Tour = p.Reserva.Tour != null ? p.Reserva.Tour.Nombre : "",
                Cliente = p.Reserva.Cliente != null
                    ? p.Reserva.Cliente.Nombre + " " + p.Reserva.Cliente.Apellido : ""
            });

        // Dos bloques, cobros y devoluciones; importes con signo permiten
        // calcular el neto con SUMA directamente en Excel o LibreOffice.
        await foreach (var devolucion in devoluciones.AsAsyncEnumerable().WithCancellation(ct))
            await EscribirMovimiento(writer, devolucion, ct);

        await writer.FlushAsync(ct);
        return new EmptyResult();
    }

    private static async Task EscribirMovimiento(
        StreamWriter writer, MovimientoFinancieroDto m, CancellationToken ct)
    {
        var neto = m.Tipo == "Reembolso" ? -m.Monto : m.Monto;
        string[] celdas = [
            m.Tipo,
            m.FechaRegistroUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            m.ReservaId.ToString(CultureInfo.InvariantCulture),
            m.Id.ToString(CultureInfo.InvariantCulture),
            m.Cliente, m.Tour, m.MetodoPago, m.Referencia, m.RegistradoPor,
            neto.ToString("0.00", CultureInfo.InvariantCulture)
        ];
        await writer.WriteLineAsync(string.Join(";", celdas.Select(EscaparCelda)).AsMemory(), ct);
    }

    private static string EscaparCelda(string? valor)
    {
        // Proteger datos introducidos por usuarios contra CSV/Formula Injection:
        // comillas dobles, salto de línea, tabulaciones y separador.
        var limpio = (valor ?? "").Replace("\r", " ").Replace("\n", " ")
            .Replace("\t", " ").Replace("\0", " ");
        var primero = limpio.TrimStart();
        if (primero.Length > 0 && "=+-@".Contains(primero[0]))
            limpio = "'" + limpio;
        return "\"" + limpio.Replace("\"", "\"\"") + "\"";
    }

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
