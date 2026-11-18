using System.Net;
using System.Net.Mail;
using System.Text;

namespace AgenciaToursAPI.Services;

public sealed class CorreoRecuperacionService(IConfiguration config)
{
    // Una configuración Recovery completa puede conservar su propio remitente.
    // En caso contrario, reutilizamos el SMTP de Notifications como un conjunto,
    // sin mezclar credenciales de diferentes cuentas.
    private bool SmtpCompleto(string prefijo) =>
        new[] { "SmtpHost", "SmtpUser", "SmtpPassword", "FromEmail" }
            .All(k => !string.IsNullOrWhiteSpace(config[prefijo + ":" + k]));

    private string? Prefijo => SmtpCompleto("Recovery") ? "Recovery" :
        SmtpCompleto("Notifications") ? "Notifications" : null;

    private string? BaseUrl => string.IsNullOrWhiteSpace(config["Recovery:PublicBaseUrl"])
        ? config["EmailVerification:PublicBaseUrl"] : config["Recovery:PublicBaseUrl"];

    private bool UrlValida => Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || (uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp)) &&
        string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) &&
        string.IsNullOrEmpty(uri.Fragment);

    public bool Configurado => Prefijo is not null && UrlValida;

    public async Task EnviarAsync(string destinatario, string token)
    {
        var prefijo = Prefijo;
        if (prefijo is null || !UrlValida) throw new InvalidOperationException("SMTP o URL de recuperación no configurados.");
        var destino = BaseUrl!.TrimEnd('/') + "/cuenta/restablecer?token=" + Uri.EscapeDataString(token);
        var enlaceHtml = WebUtility.HtmlEncode(destino);

        using var mensaje = new MailMessage
        {
            From = new MailAddress(config[prefijo + ":FromEmail"]!, "TourSmart RD"),
            Subject = "TourSmart RD - Restablecer contraseña",
            Body = "Si solicitaste recuperar tu cuenta de TourSmart RD, abre este enlace (válido 15 minutos y de un solo uso):\n\n" +
                   destino + "\n\nSi no lo solicitaste, ignora este mensaje.\n",
            IsBodyHtml = false,
            BodyEncoding = Encoding.UTF8
        };

        var html = $"""
            <!doctype html><html lang="es"><body style="margin:0;background:#F0F5F7;font-family:Arial,sans-serif;color:#163543">
            <table role="presentation" width="100%"><tr><td align="center" style="padding:32px 12px">
            <table role="presentation" width="100%" style="max-width:580px;background:#fff;border-radius:16px;overflow:hidden">
            <tr><td style="padding:28px;background:#0B3948;color:white;font-size:26px;font-weight:bold">✦ TourSmart <span style="color:#89E5AD">RD</span></td></tr>
            <tr><td style="padding:28px;line-height:1.7"><p style="color:#147D52;font-size:12px;font-weight:bold">SEGURIDAD DE TU CUENTA</p>
            <h1 style="color:#0B3948;font-size:25px">Restablece tu contraseña</h1>
            <p>Recibimos una solicitud para recuperar tu cuenta de viajero.</p>
            <p><a href="{enlaceHtml}" style="display:inline-block;padding:14px 22px;background:#0B3948;color:#fff;border-radius:8px;text-decoration:none;font-weight:bold">Crear nueva contraseña</a></p>
            <p>El enlace es válido durante 15 minutos y solo puede utilizarse una vez.</p>
            <p>Si no solicitaste el cambio, ignora el mensaje. Nunca compartas este enlace.</p></td></tr>
            <tr><td style="padding:20px;background:#E8F1F3;text-align:center;font-size:12px">TourSmart RD · Seguridad de tu cuenta</td></tr>
            </table></td></tr></table></body></html>
            """;
        mensaje.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, "text/html"));
        mensaje.To.Add(new MailAddress(destinatario));
        using var cliente = new SmtpClient(config[prefijo + ":SmtpHost"]!, config.GetValue(prefijo + ":SmtpPort", 587))
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(config[prefijo + ":SmtpUser"], config[prefijo + ":SmtpPassword"]),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await cliente.SendMailAsync(mensaje, timeout.Token);
    }
}
