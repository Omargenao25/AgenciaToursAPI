using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Models;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;

namespace AgenciaToursAPI.Controllers;

[ApiController]
[Route("api/MiCuenta")]
public sealed class MiCuentaController(
    ApplicationDbContext db,
    IPasswordHasher<Usuario> passwordHasher) : ControllerBase
{
    private async Task<int?> ClienteAutenticado()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return null;
        return await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == id && u.Rol == Roles.Cliente)
            .Select(u => u.ClienteId)
            .SingleOrDefaultAsync();
    }

    [Authorize(Roles = Roles.Cliente)]
    [HttpGet("sesion")]
    public IActionResult ValidarSesion() => NoContent();

    [Authorize(Roles = Roles.Cliente)]
    [HttpPost("cambiar-contrasena")]
    public async Task<IActionResult> CambiarContrasena(CambiarContrasenaDto dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        var usuario = await db.Usuarios
            .SingleOrDefaultAsync(u => u.Id == usuarioId && u.Rol == Roles.Cliente);
        if (usuario is null) return Unauthorized();

        var resultado = passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, dto.ContrasenaActual);
        if (resultado == PasswordVerificationResult.Failed)
            return BadRequest("La contraseña actual es incorrecta.");

        if (string.Equals(dto.ContrasenaActual, dto.ContrasenaNueva, StringComparison.Ordinal))
            return BadRequest("La contraseña nueva debe ser diferente a la actual.");

        if (dto.ContrasenaNueva.Length < 10 ||
            !dto.ContrasenaNueva.Any(char.IsUpper) ||
            !dto.ContrasenaNueva.Any(char.IsLower) ||
            !dto.ContrasenaNueva.Any(char.IsDigit) ||
            !dto.ContrasenaNueva.Any(c => !char.IsLetterOrDigit(c)))
            return BadRequest("La nueva contraseña debe tener al menos 10 caracteres, mayúscula, minúscula, número y símbolo.");

        // Update atómico con protección contra cambios concurrentes de contraseña:
        // el hash anterior debe seguir vigente para autorizar la sustitución.
        var nuevoHash = passwordHasher.HashPassword(usuario, dto.ContrasenaNueva);
        var nuevaVersion = Guid.NewGuid().ToString("N");
        var filas = await db.Usuarios
            .Where(u => u.Id == usuarioId && u.PasswordHash == usuario.PasswordHash
                && u.VersionSesion == usuario.VersionSesion)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.PasswordHash, nuevoHash)
                .SetProperty(u => u.VersionSesion, nuevaVersion));
        if (filas != 1)
            return Conflict("La contraseña cambió durante la operación. Inicia sesión de nuevo.");

        // Los JWT previos quedan revocados por OnTokenValidated.
        return NoContent();
    }

    // Las consultas y cambios se basan exclusivamente en la identidad JWT;
    // no se acepta un IdCliente arbitrario enviado por el navegador.
    [Authorize(Roles = Roles.Cliente)]
    [HttpGet("perfil")]
    public async Task<IActionResult> MiPerfil()
    {
        var clienteId = await ClienteAutenticado();
        if (clienteId is null)
            return Conflict("Esta cuenta no está vinculada a una ficha de cliente verificada.");

        var perfil = await db.Clientes.AsNoTracking()
            .Where(c => c.Id == clienteId.Value)
            .Select(c => new MiPerfilRespuestaDto
            {
                Nombre = c.Nombre,
                Apellido = c.Apellido,
                Telefono = c.Telefono,
                Correo = c.Correo,
                TipoDocumento = c.TipoDocumento,
                NumeroDocumento = c.NumeroDocumento,
                PaisEmisorDocumento = c.PaisEmisorDocumento
            }).SingleOrDefaultAsync();

        return perfil is null ? NotFound("No se encontró tu ficha de cliente.") : Ok(perfil);
    }

    [Authorize(Roles = Roles.Cliente)]
    [HttpPut("perfil")]
    public async Task<IActionResult> ActualizarMiPerfil(ActualizarMiPerfilDto dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        var nombre = dto.Nombre.Trim();
        var apellido = dto.Apellido.Trim();
        var telefono = dto.Telefono.Trim();
        if (nombre.Length == 0 || apellido.Length == 0 || telefono.Length == 0)
            return BadRequest("Nombre, apellido y teléfono son obligatorios.");
        if ((nombre + " " + apellido).Length > 100)
            return BadRequest("El nombre completo no puede superar los 100 caracteres.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var usuario = await db.Usuarios
            .Include(u => u.Cliente)
            .SingleOrDefaultAsync(u => u.Id == usuarioId && u.Rol == Roles.Cliente);

        if (usuario?.ClienteId is null || usuario.Cliente is null)
            return Conflict("Esta cuenta no está vinculada a una ficha de cliente verificada.");

        // Ni el documento ni el correo ni el rol pueden modificarse mediante este endpoint.
        usuario.Cliente.Nombre = nombre;
        usuario.Cliente.Apellido = apellido;
        usuario.Cliente.Telefono = telefono;
        usuario.Nombre = nombre + " " + apellido;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    [HttpPost("registrar")]
    [AllowAnonymous]
    public async Task<IActionResult> Registrar(RegistroClienteWebDto dto)
    {
        var correo = dto.Correo.Trim().ToLowerInvariant();
        var numero = dto.NumeroDocumento.Trim().ToUpperInvariant();
        var pais = dto.PaisEmisorDocumento.Trim();
        var tipo = dto.TipoDocumento.Trim();

        if (await db.Usuarios.AnyAsync(u => u.Correo == correo) ||
            await db.Clientes.AnyAsync(c => c.Correo == correo))
            return Conflict("El correo ya está registrado. Contacta a la agencia si necesitas vincular una cuenta existente.");

        if (await db.Clientes.AnyAsync(c => c.TipoDocumento == tipo &&
            c.PaisEmisorDocumento == pais && c.NumeroDocumento == numero))
            return Conflict("El documento ya está registrado. Contacta a la agencia para verificar su titularidad.");

        // Registro atómico para que nunca exista una cuenta nueva sin su ficha Cliente.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var cliente = new Cliente
            {
                Nombre = dto.Nombre.Trim(), Apellido = dto.Apellido.Trim(),
                Correo = correo, Telefono = dto.Telefono.Trim(),
                TipoDocumento = tipo, NumeroDocumento = numero,
                PaisEmisorDocumento = pais,
                Cedula = tipo == "CedulaDominicana" ? numero : null
            };
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync();

            var usuario = new Usuario
            {
                Nombre = cliente.Nombre + " " + cliente.Apellido,
                Correo = correo, Rol = Roles.Cliente,
                ClienteId = cliente.Id
            };
            usuario.PasswordHash = passwordHasher.HashPassword(usuario, dto.Contrasena);
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return StatusCode(StatusCodes.Status201Created);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return Conflict("El correo o documento ya existe. Comprueba tus datos o contacta a la agencia.");
        }
    }

    [Authorize(Roles = Roles.Cliente)]
    [HttpGet("reservas")]
    public async Task<IActionResult> MisReservas()
    {
        var clienteId = await ClienteAutenticado();
        if (clienteId is null) return Conflict("Tu cuenta todavía no está vinculada a una ficha de cliente verificada.");

        var reservas = await db.Reservas.AsNoTracking()
            .Where(r => r.ClienteId == clienteId.Value)
            .OrderByDescending(r => r.FechaReserva)
            .Select(r => new ReservaRespuestaDto
            {
                Id = r.Id,
                Cliente = r.Cliente != null ? r.Cliente.Nombre + " " + r.Cliente.Apellido : "",
                Tour = r.Tour != null ? r.Tour.Nombre : "",
                TourId = r.TourId,
                FechaTour = r.Tour != null ? r.Tour.Fecha : null,
                HoraTour = r.Tour != null ? r.Tour.Hora : null,
                MetodoPago = r.MetodoPago != null ? r.MetodoPago.Nombre : "",
                FechaReserva = r.FechaReserva,
                CantidadPersonas = r.CantidadPersonas,
                PrecioPorPersona = r.CantidadPersonas > 0 ? Math.Round(r.Total / r.CantidadPersonas, 2) : 0,
                Total = r.Total,
                Estado = r.Estado,
                FechaCambioEstado = r.FechaCambioEstado
            }).ToListAsync();
        return Ok(reservas);
    }

    // La identidad procede del JWT y la vinculación Usuario.ClienteId.
    // La API nunca acepta ClienteId desde el navegador para cancelar.
    [Authorize(Roles = Roles.Cliente)]
    [HttpPatch("reservas/{id:int}/cancelar")]
    public async Task<IActionResult> CancelarMiReserva(int id)
    {
        if (id <= 0) return NotFound();

        var clienteId = await ClienteAutenticado();
        if (clienteId is null)
            return Conflict("Tu cuenta no está vinculada a un cliente verificado.");

        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        // Solo buscamos la reserva del propio cliente. Para cualquier otra devolvemos 404
        // sin revelar si pertenece a otra cuenta.
        var reserva = await db.Reservas
            .FromSqlInterpolated($"SELECT * FROM dbo.Reservas WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id} AND ClienteId = {clienteId.Value}")
            .SingleOrDefaultAsync();
        if (reserva is null) return NotFound("No se encontró la reserva solicitada.");

        if (reserva.Estado == EstadosReserva.Cancelada)
            return Conflict("Esta reserva ya estaba cancelada.");

        if (reserva.Estado is not (EstadosReserva.Pendiente or EstadosReserva.Confirmada))
            return Conflict("El estado de esta reserva no permite cancelación.");

        // Protege las mismas reglas de tiempo y plazas que las operaciones administrativas.
        var tour = await db.Tours
            .FromSqlInterpolated($"SELECT * FROM dbo.Tours WITH (UPDLOCK, HOLDLOCK) WHERE Id = {reserva.TourId}")
            .SingleOrDefaultAsync();
        if (tour is null) return NotFound("El tour asociado ya no existe.");
        if (DateTime.Now >= tour.Fecha.Date.Add(tour.Hora))
            return Conflict("No se puede cancelar después del inicio del tour.");

        reserva.Estado = EstadosReserva.Cancelada;
        reserva.FechaCambioEstado = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();

        // Al excluir las reservas canceladas del cálculo de ocupación, libera sus plazas.
        // Conservar el registro permite auditar la operación.
        return NoContent();
    }

    [Authorize(Roles = Roles.Cliente)]
    [HttpGet("metodos-pago")]
    public async Task<IActionResult> MetodosPago()
    {
        if (await ClienteAutenticado() is null)
            return Conflict("Tu cuenta todavía no está vinculada a una ficha de cliente verificada.");
        return Ok(await db.MetodosPago.AsNoTracking()
            .Select(x => new { x.Id, x.Nombre }).ToListAsync());
    }

    public sealed class CrearMiReservaDto
    {
        [Range(1, int.MaxValue)] public int TourId { get; set; }
        [Range(1, int.MaxValue)] public int MetodoPagoId { get; set; }
        [Range(1, 100)] public int CantidadPersonas { get; set; }
    }

    [Authorize(Roles = Roles.Cliente)]
    [HttpPost("reservas")]
    public async Task<IActionResult> CrearMiReserva(CrearMiReservaDto dto)
    {
        var clienteId = await ClienteAutenticado();
        if (clienteId is null) return Conflict("Tu cuenta no está vinculada a un cliente verificado.");

        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var tour = await db.Tours
            .FromSqlInterpolated($"SELECT * FROM dbo.Tours WITH (UPDLOCK, HOLDLOCK) WHERE Id = {dto.TourId}")
            .SingleOrDefaultAsync();
        if (tour is null) return NotFound("El tour no existe.");
        if (DateTime.Now >= tour.Fecha.Date.Add(tour.Hora))
            return Conflict("El tour ya comenzó.");

        if (!await db.MetodosPago.AnyAsync(m => m.Id == dto.MetodoPagoId))
            return BadRequest("El método de pago no existe.");

        var reservados = await db.Reservas
            .Where(r => r.TourId == dto.TourId && r.Estado != EstadosReserva.Cancelada)
            .SumAsync(r => (int?)r.CantidadPersonas) ?? 0;
        if ((long)reservados + dto.CantidadPersonas > tour.CapacidadMaxima)
            return Conflict($"No hay plazas suficientes. Disponibles: {Math.Max(0, tour.CapacidadMaxima - reservados)}.");

        var reserva = new Reserva
        {
            ClienteId = clienteId.Value, TourId = dto.TourId,
            MetodoPagoId = dto.MetodoPagoId, CantidadPersonas = dto.CantidadPersonas,
            FechaReserva = DateTime.Now, Estado = EstadosReserva.Pendiente,
            Total = Math.Round(tour.Precio * dto.CantidadPersonas, 2)
        };
        db.Reservas.Add(reserva);
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        return StatusCode(StatusCodes.Status201Created, new { reserva.Id, reserva.Estado });
    }
}
