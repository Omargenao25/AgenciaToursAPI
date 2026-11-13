using System.ComponentModel.DataAnnotations;
using AgenciaToursAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgenciaToursAPI.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/verificacion-correo")]
public sealed class VerificacionCorreoController(VerificacionCorreoService servicio) : ControllerBase
{
    public sealed class VerificarDto
    {
        [Required, StringLength(64, MinimumLength = 64)]
        public string Token { get; set; } = "";
    }
    public sealed class ReenviarDto
    {
        [Required, EmailAddress, StringLength(150)]
        public string Correo { get; set; } = "";
    }

    [HttpPost("confirmar")]
    public async Task<IActionResult> Confirmar(VerificarDto dto, CancellationToken ct)
    {
        var ok = await servicio.ConfirmarAsync(dto.Token, ct);
        return ok ? NoContent() : BadRequest("El enlace no existe, expiró o ya fue utilizado.");
    }

    [HttpPost("reenviar")]
    public async Task<IActionResult> Reenviar(ReenviarDto dto, CancellationToken ct)
    {
        // Respuesta uniforme: no revelar qué cuentas existen.
        await servicio.ReenviarAsync(dto.Correo.Trim().ToLowerInvariant(), ct);
        return Ok(new { mensaje = "Si existe una cuenta pendiente, te enviaremos un nuevo enlace." });
    }
}
