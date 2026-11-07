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
