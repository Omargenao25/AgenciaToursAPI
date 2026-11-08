using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers;

[ApiController]
[Authorize(Roles = Roles.Personal)]
[Route("api/tours/{tourId:int}/manifiesto")]
public sealed class ManifiestosController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obtener(int tourId, CancellationToken ct)
    {
        if (tourId <= 0) return NotFound();
        var tour = await db.Tours.AsNoTracking()
            .Where(t => t.Id == tourId)
            .Select(t => new {
                t.Id, t.Nombre, t.Fecha, t.Hora, t.CapacidadMaxima,
                Guia = t.Guia != null ? t.Guia.Nombre : "",
                Transporte = t.Transporte != null ? t.Transporte.Tipo : ""
            })
            .SingleOrDefaultAsync(ct);
        if (tour is null) return NotFound("El tour no existe.");

        // No incluir reservas canceladas. Pagos y reembolsos se netean
        // para que saldos operativos reflejen devoluciones registradas.
        var reservas = await db.Reservas.AsNoTracking()
            .Where(r => r.TourId == tourId &&
                (r.Estado == "Confirmada" || r.Estado == "Pendiente"))
            .OrderBy(r => r.Id)
            .Select(r => new {
                r.Id, r.CantidadPersonas, r.Estado, r.Total,
                Titular = r.Cliente != null ? r.Cliente.Nombre + " " + r.Cliente.Apellido : "",
                Telefono = r.Cliente != null ? r.Cliente.Telefono : "",
                Cobrado = db.PagosReserva.Where(p => p.ReservaId == r.Id)
                    .Sum(p => (decimal?)p.Monto) ?? 0m,
                Devuelto = db.ReembolsosReserva.Where(p => p.ReservaId == r.Id)
                    .Sum(p => (decimal?)p.Monto) ?? 0m
            })
            .ToListAsync(ct);

        var filas = reservas.Select(r => new ManifiestoReservaDto {
            ReservaId = r.Id, Titular = r.Titular, Telefono = r.Telefono,
            CantidadPersonas = r.CantidadPersonas, Estado = r.Estado,
            Total = r.Total, NetoRecibido = r.Cobrado - r.Devuelto
        }).ToList();

        var confirmadas = filas.Where(r => r.Estado == "Confirmada").ToList();
        var pendientes = filas.Where(r => r.Estado == "Pendiente").ToList();

        Response.Headers.CacheControl = "private, no-store";
        return Ok(new ManifiestoTourDto {
            TourId = tour.Id, Tour = tour.Nombre, Fecha = tour.Fecha,
            Hora = tour.Hora, Guia = tour.Guia, Transporte = tour.Transporte,
            CapacidadMaxima = tour.CapacidadMaxima,
            CuposConfirmados = confirmadas.Sum(r => r.CantidadPersonas),
            CuposPendientes = pendientes.Sum(r => r.CantidadPersonas),
            Confirmadas = confirmadas,
            Pendientes = pendientes
        });
    }
}
