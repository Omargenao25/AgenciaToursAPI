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
        // Fecha y hora de Tour usan la hora local del servidor, igual que reservas.
        var ahora = DateTime.Now;
        var limite = ahora.AddHours(Math.Clamp(config.GetValue<int?>("Reminders:HoursBefore") ?? 24, 1, 72));
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var candidatos = await db.Reservas.AsNoTracking()
            .Where(r => r.Estado == EstadosReserva.Confirmada &&
                r.RecordatorioIntentadoUtc == null && r.Tour != null &&
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
                    r.Tour != null && r.Cliente != null && r.RecordatorioIntentadoUtc == null &&
                    !db.CierresOperativosTour.Any(c => c.TourId == r.TourId))
                .Select(r => new { r.Id, r.TourId, r.CantidadPersonas, Nombre = r.Cliente!.Nombre,
                    Correo = r.Cliente.Correo, Tour = r.Tour!.Nombre, r.Tour.Fecha, r.Tour.Hora })
                .SingleOrDefaultAsync(ct);
            if (item is null || string.IsNullOrWhiteSpace(item.Correo)) continue;
            var salida = item.Fecha.Date.Add(item.Hora);
            if (salida <= DateTime.Now || salida > limite) continue;

            // Reserva atómica del intento: dos instancias no envían el mismo aviso.
            // No se reintenta automáticamente porque SMTP puede aceptar un correo
            // y perderse la respuesta. Preferimos evitar duplicados.
            var marca = DateTime.UtcNow;
            var tomado = await db.Reservas.Where(r => r.Id == item.Id &&
                r.Estado == EstadosReserva.Confirmada && r.RecordatorioIntentadoUtc == null &&
                r.TourId == item.TourId && r.Tour != null &&
                r.Tour.Fecha == item.Fecha && r.Tour.Hora == item.Hora &&
                !db.CierresOperativosTour.Any(c => c.TourId == r.TourId))
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.RecordatorioIntentadoUtc, marca), ct);
            if (tomado != 1) continue;
            try
            {
                // Segunda comprobación para reducir el riesgo de enviar tras cancelar.
                if (!await db.Reservas.AnyAsync(r => r.Id == item.Id &&
                    r.Estado == EstadosReserva.Confirmada && r.TourId == item.TourId &&
                    r.Tour != null && r.Tour.Fecha == item.Fecha && r.Tour.Hora == item.Hora &&
                    !db.CierresOperativosTour.Any(c => c.TourId == r.TourId), ct)) continue;
                if (salida <= DateTime.Now) continue;
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
                await db.Reservas.Where(r => r.Id == item.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.RecordatorioEnviadoUtc, DateTime.UtcNow), ct);
                logger.LogInformation("Recordatorio aceptado por SMTP para reserva {Id}.", item.Id);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger.LogWarning("Intento de recordatorio sin confirmación SMTP para reserva {Id}: {Tipo}", item.Id, ex.GetType().Name);
            }
        }
    }
}
