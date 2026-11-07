using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Models;
using AgenciaToursAPI.Security;
using AgenciaToursAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace AgenciaToursAPI.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/Recuperacion")]
public sealed class RecuperacionController(
    ApplicationDbContext db,
    CorreoRecuperacionService correoService,
    IPasswordHasher<Usuario> passwordHasher,
    ILogger<RecuperacionController> logger) : ControllerBase
{
    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private static bool ClaveFuerte(string clave) =>
        clave.Length is >= 10 and <= 100 &&
        clave.Any(char.IsUpper) && clave.Any(char.IsLower) &&
        clave.Any(char.IsDigit) && clave.Any(ch => !char.IsLetterOrDigit(ch));

    [HttpPost("solicitar")]
    public async Task<IActionResult> Solicitar(SolicitarRecuperacionDto dto, CancellationToken ct)
    {
        // Respuesta uniforme: no revelar existencia de cuentas ni SMTP configurado.
        const string respuesta = "Si existe una cuenta de viajero con ese correo, recibirás instrucciones de recuperación.";
        var normalizado = dto.Correo.Trim().ToLowerInvariant();

        var usuario = await db.Usuarios.AsNoTracking()
            .Where(u => u.Correo == normalizado && u.Rol == Roles.Cliente)
            .Select(u => new { u.Id, u.Correo })
            .SingleOrDefaultAsync(ct);
        if (usuario is null) return Ok(new { mensaje = respuesta });

        var ahora = DateTime.UtcNow;
        var reciente = await db.RecuperacionesContrasena
            .AnyAsync(r => r.UsuarioId == usuario.Id &&
                 r.CreadoUtc > ahora.AddMinutes(-2), ct);
        if (reciente || !correoService.Configurado)
            return Ok(new { mensaje = respuesta });

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var registro = new RecuperacionContrasena
        {
            UsuarioId = usuario.Id,
            TokenHash = Hash(token),
            CreadoUtc = ahora,
            ExpiraUtc = ahora.AddMinutes(15)
        };
        db.RecuperacionesContrasena.Add(registro);
        await db.SaveChangesAsync(ct);

        try
        {
            await correoService.EnviarAsync(usuario.Correo, token);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Mismo resultado HTTP para cuentas existentes o inexistentes aun cuando SMTP falle.
            // No registrar el token, el destinatario ni el contenido del mensaje.
            logger.LogWarning("No fue posible enviar un correo de recuperación: {Tipo}", ex.GetType().Name);
        }
        return Ok(new { mensaje = respuesta });
    }

    [HttpPost("restablecer")]
    public async Task<IActionResult> Restablecer(RestablecerContrasenaDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Token) || !Regex.IsMatch(dto.Token, "^[A-Fa-f0-9]{64}$"))
            return BadRequest("Enlace inválido o vencido.");
        if (!ClaveFuerte(dto.ContrasenaNueva))
            return BadRequest("La nueva contraseña requiere 10 caracteres, mayúscula, minúscula, número y símbolo.");

        var tokenHash = Hash(dto.Token.ToUpperInvariant());
        // La transacción y el bloqueo serializable evitan reutilización concurrente.
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);
        var ahora = DateTime.UtcNow;
        var recuperacion = await db.RecuperacionesContrasena
            .FromSqlInterpolated($"SELECT * FROM dbo.RecuperacionesContrasena WITH (UPDLOCK, HOLDLOCK) WHERE TokenHash = {tokenHash}")
            .SingleOrDefaultAsync(ct);
        if (recuperacion is null || recuperacion.ConsumidoUtc.HasValue ||
            recuperacion.ExpiraUtc <= ahora)
            return BadRequest("Enlace inválido o vencido.");

        var usuario = await db.Usuarios
            .SingleOrDefaultAsync(u => u.Id == recuperacion.UsuarioId && u.Rol == Roles.Cliente, ct);
        if (usuario is null) return BadRequest("Enlace inválido o vencido.");

        if (passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, dto.ContrasenaNueva) !=
            PasswordVerificationResult.Failed)
            return BadRequest("La contraseña nueva debe ser diferente a la anterior.");

        usuario.PasswordHash = passwordHasher.HashPassword(usuario, dto.ContrasenaNueva);
        usuario.VersionSesion = Guid.NewGuid().ToString("N");
        // Consumir todos los enlaces abiertos para esta misma cuenta.
        await db.RecuperacionesContrasena
            .Where(r => r.UsuarioId == usuario.Id && r.ConsumidoUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.ConsumidoUtc, ahora), ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return NoContent();
    }
}
