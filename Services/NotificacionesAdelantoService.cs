using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Services;

/// <summary>
/// Avisos informativos, de mejor esfuerzo, tras la revisión persistida.
/// Nunca altera la decisión financiera ni confirma una reserva.
/// </summary>
public sealed class NotificacionesAdelantoService(
    IConfiguration config,
    ApplicationDbContext db,
    ILogger<NotificacionesAdelantoService> logger)
{
    private bool Habilitado =>
        config.GetValue<bool>("Notifications:Enabled") &&
        !string.IsNullOrWhiteSpace(config["Notifications:SmtpHost"]) &&
        !string.IsNullOrWhiteSpace(config["Notifications:SmtpUser"]) &&
        !string.IsNullOrWhiteSpace(config["Notifications:SmtpPassword"]) &&
        !string.IsNullOrWhiteSpace(config["Notifications:FromEmail"]);

    public async Task AvisarRevisionAsync(long comprobanteId)
    {
        if (!Habilitado) return;
        try
        {
            var info = await db.ComprobantesTransferencia.AsNoTracking()
                .Where(c => c.Id == comprobanteId && (c.Estado == "Aprobado" || c.Estado == "Rechazado"))
                .Select(c => new
                {
                    c.Id, c.ReservaId, c.MontoReportado, c.Referencia, c.Estado, c.MotivoRevision,
                    Correo = c.Cliente.Correo,
                    Nombre = c.Cliente.Nombre,
                    Tour = c.Reserva.Tour != null ? c.Reserva.Tour.Nombre : "",
                    EstadoReserva = c.Reserva.Estado
                }).SingleOrDefaultAsync();

            if (info is null || string.IsNullOrWhiteSpace(info.Correo)) return;
            var aprobado = info.Estado == "Aprobado";
            var titulo = aprobado ? "Adelanto verificado" : "Comprobante no aprobado";
            var mensaje = aprobado
                ? "Verificamos la transferencia y registramos el importe como abono recibido. La confirmación de tu reserva es un paso separado; consulta su estado en TourSmart RD."
                : "No pudimos validar la transferencia reportada. Revisa el motivo y contacta a la agencia si necesitas ayuda.";
            var motivo = aprobado ? null : info.MotivoRevision;
            var importe = info.MontoReportado.ToString("N2", CultureInfo.GetCultureInfo("es-DO"));
            var plain = $"Hola {info.Nombre},\n\n{mensaje}\n\n" +
                $"Excursión: {info.Tour}\nReserva: #{info.ReservaId}\nComprobante: #{info.Id}\n" +
                $"Importe reportado: RD$ {importe}\nReferencia: {info.Referencia}\n" +
                $"Estado del comprobante: {info.Estado}\nEstado de reserva: {info.EstadoReserva}\n" +
                (motivo is null ? "" : $"Motivo de revisión: {motivo}\n") +
                "\nEl comprobante no es un recibo bancario ni garantiza una reserva confirmada.\nTourSmart RD";

            static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
            var color = aprobado ? "#147D52" : "#B43E43";
            var estado = aprobado ? "ABONO VERIFICADO" : "COMPROBANTE RECHAZADO";
            var motivoHtml = motivo is null ? "" :
                $"<tr><td style=\"padding:13px 18px;background:#FFF5F3;color:#7B2830;font-size:13px;line-height:1.6;\"><strong>Motivo:</strong> {E(motivo)}</td></tr>";
            var html = $"""
                <!doctype html><html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
                <body style="margin:0;background:#F0F5F7;font-family:Arial,Helvetica,sans-serif;color:#163543;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#F0F5F7"><tr><td align="center" style="padding:32px 12px">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:580px;background:#fff;border-radius:16px;overflow:hidden">
                <tr><td style="padding:28px 30px;background:#0B3948;color:#fff;font-size:26px;font-weight:bold">✦ TourSmart <span style="color:#89E5AD">RD</span><div style="font-size:11px;letter-spacing:2px;color:#DAEBEC;margin-top:8px">EXPERIENCIAS &amp; EXCURSIONES</div></td></tr>
                <tr><td style="padding:28px 30px 14px"><div style="font-size:12px;color:{color};font-weight:bold">{estado}</div>
                <h1 style="font-size:25px;line-height:1.25;color:#0B3948;margin:12px 0">{E(titulo)}</h1>
                <p style="font-size:15px;line-height:1.7;color:#405C68">Hola, {E(info.Nombre)}. {E(mensaje)}</p></td></tr>
                <tr><td style="padding:12px 30px 22px"><table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#F7FAFB;border:1px solid #DCE8EB;border-radius:12px">
                <tr><td style="padding:17px 18px;font-size:18px;font-weight:bold;color:#0B3948">{E(info.Tour)}</td></tr>
                <tr><td style="padding:9px 18px;font-size:14px">Reserva: <strong>#{info.ReservaId}</strong> · Comprobante: <strong>#{info.Id}</strong></td></tr>
                <tr><td style="padding:9px 18px;font-size:14px">Monto reportado: <strong>RD$ {E(importe)}</strong></td></tr>
                <tr><td style="padding:9px 18px;font-size:14px">Referencia: <strong>{E(info.Referencia)}</strong></td></tr>
                <tr><td style="padding:9px 18px;font-size:14px">Reserva: <strong>{E(info.EstadoReserva)}</strong></td></tr>
                {motivoHtml}
                </table></td></tr>
                <tr><td style="padding:5px 30px 28px;color:#607A84;font-size:13px;line-height:1.6">Este mensaje es informativo y no sustituye un comprobante bancario. Una reserva permanece pendiente hasta que la agencia la confirme.</td></tr>
                <tr><td style="background:#E8F1F3;padding:20px;text-align:center;color:#54717C;font-size:12px">TourSmart RD · Tu próxima aventura comienza aquí.</td></tr>
                </table></td></tr></table></body></html>
                """;

            using var correo = new MailMessage
            {
                From = new MailAddress(config["Notifications:FromEmail"]!, "TourSmart RD"),
                Subject = "TourSmart RD - " + titulo + " - Reserva #" + info.ReservaId,
                Body = plain,
                IsBodyHtml = false
            };
            correo.To.Add(new MailAddress(info.Correo));
            correo.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                html, Encoding.UTF8, "text/html"));

            using var smtp = new SmtpClient(config["Notifications:SmtpHost"]!,
                config.GetValue<int?>("Notifications:SmtpPort") ?? 587)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(
                    config["Notifications:SmtpUser"], config["Notifications:SmtpPassword"]),
                Timeout = 8000
            };
            await smtp.SendMailAsync(correo);
            logger.LogInformation("Aviso de comprobante {ComprobanteId} ({Estado}) enviado.", comprobanteId, info.Estado);
        }
        catch (Exception ex)
        {
            // Gmail nunca debe poder deshacer una revisión o un abono ya confirmado en SQL Server.
            logger.LogWarning("No se pudo enviar el correo del comprobante {ComprobanteId}: {Tipo}",
                comprobanteId, ex.GetType().Name);
        }
    }
}
