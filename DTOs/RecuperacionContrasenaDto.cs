using System.ComponentModel.DataAnnotations;

namespace AgenciaToursAPI.DTOs;

public sealed class SolicitarRecuperacionDto
{
    [Required, EmailAddress, StringLength(150)]
    public string Correo { get; set; } = "";
}

public sealed class RestablecerContrasenaDto
{
    [Required, StringLength(256)]
    public string Token { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 10)]
    public string ContrasenaNueva { get; set; } = "";
}
