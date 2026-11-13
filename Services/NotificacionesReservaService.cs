using System.Net;
using System.Net.Mail;
using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Services;

/// <summary>
/// Envío opcional y de mejor esfuerzo. Nunca cambia la respuesta ni la transacción de reservas.
/// No se envían correos en desarrollo hasta configurar explícitamente Notifications:Enabled.
/// </summary>
public sealed class NotificacionesReservaService(
    IConfiguration configuracion,
    ApplicationDbContext db,
    ILogger<NotificacionesReservaService> logger)
{
    public bool Habilitado =>
        configuracion.GetValue<bool>("Notifications:Enabled") &&
        !string.IsNullOrWhiteSpace(configuracion["Notifications:SmtpHost"]) &&
        !string.IsNullOrWhiteSpace(configuracion["Notifications:SmtpUser"]) &&
        !string.IsNullOrWhiteSpace(configuracion["Notifications:SmtpPassword"]) &&
        !string.IsNullOrWhiteSpace(configuracion["Notifications:FromEmail"]);

    public async Task AvisarAsync(int reservaId, string evento)
    {
        if (!Habilitado) return;
        if (evento is not ("Creada" or "Confirmada" or "Cancelada")) return;

        try
        {
            var info = await db.Reservas.AsNoTracking()
                .Where(r => r.Id == reservaId)
                .Select(r => new
                {
                    r.Id, r.CantidadPersonas, r.Total, r.Estado,
                    Correo = r.Cliente != null ? r.Cliente.Correo : "",
                    Nombre = r.Cliente != null ? r.Cliente.Nombre : "",
                    Tour = r.Tour != null ? r.Tour.Nombre : "",
                    Fecha = r.Tour != null ? (DateTime?)r.Tour.Fecha : null,
                    Hora = r.Tour != null ? (TimeSpan?)r.Tour.Hora : null
                }).SingleOrDefaultAsync();

            if (info is null || string.IsNullOrWhiteSpace(info.Correo)) return;

            var titulo = evento switch
            {
                "Creada" => "Solicitud de reserva recibida",
                "Confirmada" => "Tu reserva está confirmada",
                _ => "Tu reserva fue cancelada"
            };

            var estado = evento switch
            {
                "Creada" => "Tu solicitud está pendiente. La agencia debe verificar el adelanto requerido antes de confirmarla.",
                "Confirmada" => "Tu reserva fue confirmada. Conserva este mensaje y revisa los detalles de tu excursión.",
                _ => "Tu reserva fue cancelada. Si necesitas información sobre devoluciones, contacta a la agencia."
            };

            var fecha = info.Fecha?.ToString("dd/MM/yyyy") ?? "Por confirmar";
            var hora = info.Hora?.ToString(@"hh\:mm") ?? "Por confirmar";
            var body = $"Hola {info.Nombre},\n\n{estado}\n\n" +
                $"Reserva: #{info.Id}\nExcursión: {info.Tour}\n" +
                $"Fecha: {fecha}\nHora: {hora}\nPasajeros: {info.CantidadPersonas}\n" +
                $"Importe total: RD$ {info.Total:N2}\nEstado actual: {info.Estado}\n\n" +
                "TourSmart RD\nEste correo es informativo y no constituye un recibo de pago.";

            using var mensaje = new MailMessage
            {
                From = new MailAddress(configuracion["Notifications:FromEmail"]!, "TourSmart RD"),
                Subject = "TourSmart RD - " + titulo + " #" + info.Id,
                Body = body,
                IsBodyHtml = false
            };
            // HTML como vista adicional, conservando texto plano para clientes
            // que no muestran HTML. Nunca interpolar valores sin codificar.
            var html = PlantillaCorreoReserva.Crear(
                info.Nombre, info.Tour, info.Id, evento,
                info.Fecha, info.Hora, info.CantidadPersonas, info.Total, info.Estado);
            mensaje.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(
                    html, System.Text.Encoding.UTF8, "text/html"));
            mensaje.To.Add(new MailAddress(info.Correo));

            using var smtp = new SmtpClient(
                configuracion["Notifications:SmtpHost"]!,
                configuracion.GetValue<int?>("Notifications:SmtpPort") ?? 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(
                    configuracion["Notifications:SmtpUser"],
                    configuracion["Notifications:SmtpPassword"]),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 8000
            };

            await smtp.SendMailAsync(mensaje);
            logger.LogInformation("Aviso de {Evento} enviado para reserva {Id}.", evento, reservaId);
        }
        catch (Exception ex) when (ex is SmtpException or FormatException or InvalidOperationException
            or HttpRequestException or IOException)
        {
            logger.LogWarning("No se pudo enviar el aviso {Evento} de la reserva {Id}: {Tipo}.",
                evento, reservaId, ex.GetType().Name);
        }
        catch (Exception ex)
        {
            // No falla una reserva ya confirmada por problemas ajenos al negocio.
            logger.LogError("Falló la notificación {Evento} de la reserva {Id}. Tipo: {Tipo}.",
                evento, reservaId, ex.GetType().Name);
        }
    }
}
