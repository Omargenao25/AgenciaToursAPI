using AgenciaToursAPI.Data;
using AgenciaToursAPI.Security;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AgenciaToursAPI.Services;

/// <summary>
/// Revisa las solicitudes pendientes al iniciar la API y luego cada minuto.
/// Protege hasta dos horas adicionales los reportes realizados antes del vencimiento.
/// No considera un reporte como pago recibido; tras el margen, cancela sin pago verificado.
/// </summary>
public sealed class VencimientoReservasService(
    IServiceScopeFactory scopeFactory,
    ILogger<VencimientoReservasService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RevisarSeguro(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RevisarSeguro(stoppingToken);
    }

    private async Task RevisarSeguro(CancellationToken ct)
    {
        try
        {
            var total = await ProcesarAsync(ct);
            if (total > 0)
                logger.LogInformation("Se vencieron {Total} solicitudes sin pagos y se liberaron sus cupos.", total);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            // Un error temporal no debe detener permanentemente la API.
            logger.LogError(ex, "Error en la revisión automática de reservas pendientes.");
        }
    }

    public async Task<int> ProcesarAsync(CancellationToken ct)
    {
        var canceladas = 0;
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ahora = DateTime.UtcNow;

        // Límite por pasada para evitar retener muchos bloqueos. Las restantes
        // se procesarán en la próxima ejecución periódica.
        var ids = await db.Reservas.AsNoTracking()
            .Where(r => r.Estado == EstadosReserva.Pendiente &&
                        r.VencePendienteUtc != null &&
                        r.VencePendienteUtc <= ahora &&
                        !db.PagosReserva.Any(p => p.ReservaId == r.Id) &&
                        !db.ComprobantesTransferencia.Any(c =>
                            c.ReservaId == r.Id && c.Estado == "Pendiente" &&
                            c.CreadoUtc <= r.VencePendienteUtc &&
                            ahora < r.VencePendienteUtc.Value.AddHours(2)))
            .OrderBy(r => r.VencePendienteUtc)
            .Select(r => r.Id)
            .Take(100)
            .ToListAsync(ct);

        foreach (var id in ids)
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var reserva = await db.Reservas
                .FromSqlInterpolated($"SELECT * FROM dbo.Reservas WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
                .SingleOrDefaultAsync(ct);

            // Revalidar dentro de la transacción para evitar carreras con pagos,
            // confirmaciones y cancelaciones manuales.
            if (reserva is not null &&
                reserva.Estado == EstadosReserva.Pendiente &&
                reserva.VencePendienteUtc is not null &&
                reserva.VencePendienteUtc <= DateTime.UtcNow &&
                !await db.PagosReserva.AnyAsync(p => p.ReservaId == id, ct) &&
                !await db.ComprobantesTransferencia.AnyAsync(c =>
                    c.ReservaId == id && c.Estado == "Pendiente" &&
                    c.CreadoUtc <= reserva.VencePendienteUtc &&
                    DateTime.UtcNow < reserva.VencePendienteUtc.Value.AddHours(2), ct))
            {
                reserva.Estado = EstadosReserva.Cancelada;
                reserva.MotivoCancelacion = "VencimientoSinPago";
                // El reporte continúa registrado para auditoría, pero ya no es aprobable.
                var comprobantes = await db.ComprobantesTransferencia
                    .Where(c => c.ReservaId == id && c.Estado == "Pendiente")
                    .ToListAsync(ct);
                foreach (var comprobante in comprobantes)
                {
                    comprobante.Estado = "Rechazado";
                    comprobante.MotivoRevision = "Venció el plazo de revisión sin pago verificado. Contacta a la agencia.";
                    comprobante.RevisadoUtc = DateTime.UtcNow;
                }
                reserva.FechaCambioEstado = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                canceladas++;
            }

            await tx.CommitAsync(ct);
            db.ChangeTracker.Clear();
        }
        return canceladas;
    }
}
