using System.Net;
using System.Net.Mail;
using System.Text;
using AgenciaToursAPI.Data;
using AgenciaToursAPI.Security;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Services;

public sealed class RecordatoriosExcursionesService(
    IServiceScopeFactory scopes, IConfiguration config,
    ILogger<RecordatoriosExcursionesService> logger) : BackgroundService
{
    private bool Habilitado => config.GetValue<bool>("Reminders:Enabled") &&
        config.GetValue<bool>("Notifications:Enabled") &&
        new[] {"SmtpHost", "SmtpUser", "SmtpPassword", "FromEmail"}
            .All(k => !string.IsNullOrWhiteSpace(config["Notifications:" + k]));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try { if (Habilitado) await ProcesarAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogWarning("Error de recordatorios: {Tipo}", ex.GetType().Name); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcesarAsync(CancellationToken ct)
    {
        // Las salidas se comparan en hora dominicana, los reintentos en UTC.
        var ahora = HorarioAgencia.Ahora;
        var ahoraUtc = DateTime.UtcNow;
        var limite = ahora.AddHours(Math.Clamp(config.GetValue<int?>("Reminders:HoursBefore") ?? 24, 1, 72));
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Un lease expirado puede indicar que SMTP aceptó el envío antes
        // de que cayera el proceso. No reintentar sin revisión.
        await db.Reservas.Where(r => r.RecordatorioEstado == "Enviando" &&
                                r.RecordatorioLeaseHastaUtc < ahoraUtc)
            .ExecuteUpdateAsync(u => u
                .SetProperty(r => r.RecordatorioEstado, "Revisar")
                .SetProperty(r => r.RecordatorioLeaseHastaUtc, (DateTime?)null)
                .SetProperty(r => r.RecordatorioLeaseToken, (string?)null), ct);
        var candidatos = await db.Reservas.AsNoTracking()
            .Where(r => r.Estado == EstadosReserva.Confirmada &&
                r.RecordatorioEnviadoUtc == null && r.RecordatorioIntentos < 3 &&
                (r.RecordatorioEstado == "Pendiente" ||
                 (r.RecordatorioEstado == "Fallido" && r.RecordatorioProximoIntentoUtc <= ahoraUtc)) &&
                r.Tour != null &&
                r.Tour.Fecha >= ahora.Date && r.Tour.Fecha < limite.Date.AddDays(1) &&
                !db.CierresOperativosTour.Any(c => c.TourId == r.TourId))
            .Select(r => new { r.Id, Fecha = r.Tour!.Fecha, Hora = r.Tour.Hora })
            .ToListAsync(ct);

        foreach (var candidato in candidatos
            .Where(r => r.Fecha.Date.Add(r.Hora) > ahora && r.Fecha.Date.Add(r.Hora) <= limite)
            .OrderBy(r => r.Fecha.Date.Add(r.Hora)).ThenBy(r => r.Id).Take(50))
        {
            ct.ThrowIfCancellationRequested();
            var item = await db.Reservas.AsNoTracking()
                .Where(r => r.Id == candidato.Id && r.Estado == EstadosReserva.Confirmada &&
                    r.Tour != null && r.Cliente != null &&
                    r.RecordatorioEnviadoUtc == null && r.RecordatorioIntentos < 3 &&
                    (r.RecordatorioEstado == "Pendiente" ||
                     (r.RecordatorioEstado == "Fallido" && r.RecordatorioProximoIntentoUtc <= ahoraUtc)) &&
                    !db.CierresOperativosTour.Any(c => c.TourId == r.TourId))
                .Select(r => new { r.Id, r.TourId, r.CantidadPersonas, Nombre = r.Cliente!.Nombre,
                    Correo = r.Cliente.Correo, Tour = r.Tour!.Nombre, r.Tour.Fecha, r.Tour.Hora })
                .SingleOrDefaultAsync(ct);
            if (item is null || string.IsNullOrWhiteSpace(item.Correo)) continue;
            var salida = item.Fecha.Date.Add(item.Hora);
            if (salida <= HorarioAgencia.Ahora || salida > limite) continue;

            // Claim atómico: ninguna segunda instancia procesa el mismo envío.
            var marca = DateTime.UtcNow;
            var token = Guid.NewGuid().ToString("N");
            var tomado = await db.Reservas.Where(r => r.Id == item.Id &&
                r.Estado == EstadosReserva.Confirmada && r.RecordatorioEnviadoUtc == null &&
                r.RecordatorioIntentos < 3 &&
                (r.RecordatorioEstado == "Pendiente" ||
                 (r.RecordatorioEstado == "Fallido" && r.RecordatorioProximoIntentoUtc <= marca)) &&
                r.TourId == item.TourId && r.Tour != null &&
                r.Tour.Fecha == item.Fecha && r.Tour.Hora == item.Hora &&
                !db.CierresOperativosTour.Any(c => c.TourId == r.TourId))
                .ExecuteUpdateAsync(u => u
                    .SetProperty(r => r.RecordatorioEstado, "Enviando")
                    .SetProperty(r => r.RecordatorioIntentos, r => r.RecordatorioIntentos + 1)
                    .SetProperty(r => r.RecordatorioIntentadoUtc, marca)
                    .SetProperty(r => r.RecordatorioLeaseHastaUtc, marca.AddMinutes(2))
                    .SetProperty(r => r.RecordatorioLeaseToken, token)
                    .SetProperty(r => r.RecordatorioProximoIntentoUtc, (DateTime?)null), ct);
            if (tomado != 1) continue;
            try
            {
                // Segunda comprobación para reducir el riesgo de enviar tras cancelar.
                if (!await db.Reservas.AnyAsync(r => r.Id == item.Id &&
                    r.Estado == EstadosReserva.Confirmada && r.TourId == item.TourId &&
                    r.Tour != null && r.Tour.Fecha == item.Fecha && r.Tour.Hora == item.Hora &&
                    !db.CierresOperativosTour.Any(c => c.TourId == r.TourId), ct) ||
                    salida <= HorarioAgencia.Ahora)
                {
                    await CambiarEstadoAsync(db, item.Id, token, "Revisar", null, ct);
                    continue;
                }
                var fecha = salida.ToString("dd/MM/yyyy HH:mm");
                var contacto = config["Notifications:FromEmail"]!;
                var texto = $"Hola {item.Nombre},\n\nTu excursión {item.Tour} está próxima.\nReserva #{item.Id}\nSalida: {fecha} (hora local de la agencia)\nPasajeros: {item.CantidadPersonas}\nConfirma el punto de encuentro con la agencia, llega con anticipación y lleva tu identificación.\nContacto: {contacto}\n\nTourSmart RD";
                static string E(string? x) => WebUtility.HtmlEncode(x ?? "");
                var html = $"""
                <!doctype html><html lang="es"><body style="margin:0;background:#F0F5F7;font-family:Arial,sans-serif;color:#163543">
                <table role="presentation" width="100%"><tr><td align="center" style="padding:32px 12px">
                <table role="presentation" width="100%" style="max-width:580px;background:#fff;border-radius:16px;overflow:hidden">
                <tr><td style="padding:28px;background:#0B3948;color:white;font-size:26px;font-weight:bold">✦ TourSmart <span style="color:#89E5AD">RD</span></td></tr>
                <tr><td style="padding:28px"><p style="color:#147D52;font-weight:bold;font-size:12px">PRÓXIMA EXCURSIÓN</p>
                <h1 style="color:#0B3948;font-size:25px">¡Tu aventura está cerca!</h1>
                <p>Hola, {E(item.Nombre)}. Te recordamos los detalles de tu reserva confirmada.</p>
                <div style="background:#F7FAFB;border:1px solid #DCE8EB;padding:20px;border-radius:12px;line-height:1.8">
                <strong>{E(item.Tour)}</strong><br>Reserva #{item.Id}<br>Salida: {E(fecha)} (hora local de la agencia)<br>Pasajeros: {item.CantidadPersonas}</div>
                <p>Confirma el punto de encuentro con la agencia, llega con anticipación y lleva tu identificación.</p>
                <p>Contacto: {E(contacto)}</p></td></tr>
                <tr><td style="padding:20px;background:#E8F1F3;text-align:center">TourSmart RD · Tu próxima aventura comienza aquí.</td></tr>
                </table></td></tr></table></body></html>
                """;
                using var mensaje = new MailMessage
                {
                    From = new MailAddress(contacto, "TourSmart RD"),
                    Subject = "TourSmart RD - Tu excursión está cerca #" + item.Id,
                    Body = texto,
                    IsBodyHtml = false
                };
                mensaje.To.Add(new MailAddress(item.Correo));
                mensaje.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, "text/html"));
                using var smtp = new SmtpClient(config["Notifications:SmtpHost"]!,
                    config.GetValue<int?>("Notifications:SmtpPort") ?? 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(config["Notifications:SmtpUser"], config["Notifications:SmtpPassword"])
                };
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(20));
                await smtp.SendMailAsync(mensaje, timeout.Token);
                var guardado = await db.Reservas.Where(r => r.Id == item.Id &&
                        r.RecordatorioEstado == "Enviando" && r.RecordatorioLeaseToken == token)
                    .ExecuteUpdateAsync(u => u
                        .SetProperty(r => r.RecordatorioEnviadoUtc, DateTime.UtcNow)
                        .SetProperty(r => r.RecordatorioEstado, "Enviado")
                        .SetProperty(r => r.RecordatorioLeaseToken, (string?)null)
                        .SetProperty(r => r.RecordatorioLeaseHastaUtc, (DateTime?)null), ct);
                if (guardado == 1)
                    logger.LogInformation("Recordatorio aceptado por SMTP para reserva {Id}.", item.Id);
                else
                    logger.LogWarning("SMTP aceptó recordatorio {Id} sin poder confirmar la marca SQL.", item.Id);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (SmtpException ex) when (ex.StatusCode is SmtpStatusCode.ServiceNotAvailable or SmtpStatusCode.MailboxBusy)
            {
                var intento = await db.Reservas.AsNoTracking()
                    .Where(r => r.Id == item.Id && r.RecordatorioLeaseToken == token)
                    .Select(r => r.RecordatorioIntentos).SingleOrDefaultAsync(ct);
                var pausa = intento == 1 ? TimeSpan.FromMinutes(2) : TimeSpan.FromMinutes(5);
                var puedeReintentar = intento is > 0 and < 3 &&
                                      HorarioAgencia.Ahora.Add(pausa) < salida;
                await CambiarEstadoAsync(db, item.Id, token,
                    puedeReintentar ? "Fallido" : "Agotado",
                    puedeReintentar ? DateTime.UtcNow.Add(pausa) : null, ct);
                logger.LogWarning("SMTP devolvió un rechazo transitorio para recordatorio {Id}: {Tipo}",
                    item.Id, ex.GetType().Name);
            }
            catch (Exception ex)
            {
                // Errores sin resultado SMTP concluyente: nunca reenviar a ciegas.
                await CambiarEstadoAsync(db, item.Id, token, "Revisar", null, ct);
                logger.LogWarning("Recordatorio {Id} requiere revisión por envío incierto: {Tipo}",
                    item.Id, ex.GetType().Name);
            }
        }
    }
    private static async Task CambiarEstadoAsync(ApplicationDbContext db, int id, string token,
        string estado, DateTime? proximo, CancellationToken ct)
    {
        await db.Reservas.Where(r => r.Id == id &&
                r.RecordatorioEstado == "Enviando" && r.RecordatorioLeaseToken == token)
            .ExecuteUpdateAsync(u => u
                .SetProperty(r => r.RecordatorioEstado, estado)
                .SetProperty(r => r.RecordatorioProximoIntentoUtc, proximo)
                .SetProperty(r => r.RecordatorioLeaseHastaUtc, (DateTime?)null)
                .SetProperty(r => r.RecordatorioLeaseToken, (string?)null), ct);
    }

}
