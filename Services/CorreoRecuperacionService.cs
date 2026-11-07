using System.Net;
using System.Net.Mail;

namespace AgenciaToursAPI.Services;

public sealed class CorreoRecuperacionService(IConfiguration config)
{
    public bool Configurado =>
        !string.IsNullOrWhiteSpace(config["Recovery:SmtpHost"]) &&
        !string.IsNullOrWhiteSpace(config["Recovery:SmtpUser"]) &&
        !string.IsNullOrWhiteSpace(config["Recovery:SmtpPassword"]) &&
        !string.IsNullOrWhiteSpace(config["Recovery:FromEmail"]) &&
        !string.IsNullOrWhiteSpace(config["Recovery:PublicBaseUrl"]);

    public async Task EnviarAsync(string destinatario, string token)
    {
        if (!Configurado) throw new InvalidOperationException("SMTP no configurado.");
        var baseUrl = config["Recovery:PublicBaseUrl"]!.TrimEnd('/');
        var destino = baseUrl + "/cuenta/restablecer?token=" + Uri.EscapeDataString(token);

        using var mensaje = new MailMessage
        {
            From = new MailAddress(config["Recovery:FromEmail"]!),
            Subject = "TourSmart RD - Restablecer contraseña",
            Body = "Si solicitaste recuperar tu cuenta de TourSmart RD, abre este enlace (válido 15 minutos y de un solo uso):\n\n" +
                   destino + "\n\nSi no lo solicitaste, ignora este mensaje.\n",
            IsBodyHtml = false
        };
        mensaje.To.Add(new MailAddress(destinatario));
        using var cliente = new SmtpClient(config["Recovery:SmtpHost"]!, config.GetValue("Recovery:SmtpPort", 587))
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(config["Recovery:SmtpUser"], config["Recovery:SmtpPassword"]),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 10000
        };
        await cliente.SendMailAsync(mensaje);
    }
}
