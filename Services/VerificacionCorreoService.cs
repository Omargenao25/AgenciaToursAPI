using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using AgenciaToursAPI.Security;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Services;

public sealed class VerificacionCorreoService(
    ApplicationDbContext db,
    IConfiguration config,
    ILogger<VerificacionCorreoService> logger)
{
    public bool Habilitado =>
        config.GetValue<bool>("EmailVerification:Enabled") &&
        !string.IsNullOrWhiteSpace(config["Notifications:SmtpHost"]) &&
        !string.IsNullOrWhiteSpace(config["Notifications:SmtpUser"]) &&
        !string.IsNullOrWhiteSpace(config["Notifications:SmtpPassword"]) &&
        !string.IsNullOrWhiteSpace(config["Notifications:FromEmail"]) &&
        Uri.TryCreate(config["EmailVerification:PublicBaseUrl"], UriKind.Absolute, out var u) &&
        (u.Scheme == Uri.UriSchemeHttps || (u.IsLoopback && u.Scheme == Uri.UriSchemeHttp));

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task EnviarSiProcedeAsync(int usuarioId, CancellationToken ct = default)
    {
        if (!Habilitado) return;
        var ahora = DateTime.UtcNow;
        var usuario = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId && u.Rol == Roles.Cliente && !u.CorreoConfirmado)
            .Select(u => new { u.Id, u.Correo, u.Nombre }).SingleOrDefaultAsync(ct);
        if (usuario is null) return;

        // Rate limit persistente para evitar correo abusivo incluso tras reiniciar la API:
        // mínimo 2 minutos entre envíos y máximo 5 intentos por día y cuenta.
        var intentos = await db.VerificacionesCorreo.AsNoTracking()
            .Where(v => v.UsuarioId == usuario.Id && v.CreadoUtc > ahora.AddDays(-1))
            .Select(v => v.CreadoUtc).ToListAsync(ct);
        if (intentos.Count >= 5 || intentos.Any(x => x > ahora.AddMinutes(-2))) return;

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.VerificacionesCorreo.Add(new VerificacionCorreo
        {
            UsuarioId = usuario.Id, TokenHash = Hash(token),
            CreadoUtc = ahora, ExpiraUtc = ahora.AddMinutes(30)
        });
        await db.SaveChangesAsync(ct);

        var enlace = config["EmailVerification:PublicBaseUrl"]!.TrimEnd('/') +
            "/viajero/verificar-correo?token=" + Uri.EscapeDataString(token);
        using var mensaje = new MailMessage
        {
            From = new MailAddress(config["Notifications:FromEmail"]!, "TourSmart RD"),
            Subject = "TourSmart RD - Verifica tu correo",
            Body = "Hola " + usuario.Nombre + ",\n\nConfirma tu correo electrónico para activar tu cuenta de viajero:\n\n" +
                enlace + "\n\nEste enlace caduca en 30 minutos y se puede usar una sola vez.\n" +
                "Si no creaste esta cuenta, ignora el mensaje.\n\nTourSmart RD",
            IsBodyHtml = false
        };
        mensaje.To.Add(new MailAddress(usuario.Correo));
        try
        {
            using var smtp = new SmtpClient(config["Notifications:SmtpHost"]!, config.GetValue<int?>("Notifications:SmtpPort") ?? 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(config["Notifications:SmtpUser"], config["Notifications:SmtpPassword"]),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 8000
            };
            await smtp.SendMailAsync(mensaje);
            logger.LogInformation("Se solicitó verificación de correo para usuario {UsuarioId}.", usuario.Id);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException or IOException)
        {
            logger.LogWarning("Falló envío de verificación al usuario {UsuarioId}: {Tipo}.", usuario.Id, ex.GetType().Name);
        }
    }

    public async Task<bool> ConfirmarAsync(string token, CancellationToken ct)
    {
        if (!Habilitado || string.IsNullOrWhiteSpace(token) ||
            !System.Text.RegularExpressions.Regex.IsMatch(token, "^[A-Fa-f0-9]{64}$"))
            return false;

        var hash = Hash(token.ToUpperInvariant());
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var registro = await db.VerificacionesCorreo
            .FromSqlInterpolated($"SELECT * FROM dbo.VerificacionesCorreo WITH (UPDLOCK, HOLDLOCK) WHERE TokenHash = {hash}")
            .SingleOrDefaultAsync(ct);
        if (registro is null || registro.ConsumidoUtc.HasValue || registro.ExpiraUtc <= DateTime.UtcNow)
            return false;
        var usuario = await db.Usuarios.SingleOrDefaultAsync(x => x.Id == registro.UsuarioId && x.Rol == Roles.Cliente, ct);
        if (usuario is null) return false;
        if (usuario.CorreoConfirmado) return false;

        usuario.CorreoConfirmado = true;
        var ahora = DateTime.UtcNow;
        await db.VerificacionesCorreo.Where(v => v.UsuarioId == usuario.Id && v.ConsumidoUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.ConsumidoUtc, ahora), ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task ReenviarAsync(string correo, CancellationToken ct)
    {
        if (!Habilitado) return;
        var usuario = await db.Usuarios.AsNoTracking()
            .Where(x => x.Rol == Roles.Cliente && !x.CorreoConfirmado && x.Correo == correo)
            .Select(x => (int?)x.Id).SingleOrDefaultAsync(ct);
        if (usuario.HasValue) await EnviarSiProcedeAsync(usuario.Value, ct);
    }
}
