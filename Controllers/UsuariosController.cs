using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Models;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgenciaToursAPI.Controllers
{
    [Authorize(Roles = Roles.Administrador)]
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public UsuariosController(
            ApplicationDbContext context,
            IPasswordHasher<Usuario> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

      
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioRespuestaDto>>>
            GetUsuarios()
        {
            var usuarios = await _context.Usuarios
                .AsNoTracking()
                .Select(u => new UsuarioRespuestaDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Correo = u.Correo,
                    Rol = u.Rol
                })
                .ToListAsync();

            return Ok(usuarios);
        }

      
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioRespuestaDto>>
            GetUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new UsuarioRespuestaDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Correo = u.Correo,
                    Rol = u.Rol
                })
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                return NotFound("El usuario no existe.");
            }

            return Ok(usuario);
        }

        // Registro alternativo retirado: generaba cuentas sin ficha Cliente y
        // evitaba el proceso de verificación de correo.
        // Los viajeros deben registrarse en api/MiCuenta/registrar.
        [HttpPost]
        public IActionResult PostUsuario() =>
            StatusCode(StatusCodes.Status410Gone, new
            {
                mensaje = "Ruta de registro retirada. Utiliza /viajero/registro o api/MiCuenta/registrar."
            });


        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(
            int id,
            ActualizarUsuarioDto dto)
        {
            var usuario = await _context.Usuarios
                .FindAsync(id);

            if (usuario == null)
            {
                return NotFound("El usuario no existe.");
            }

            var correoNormalizado = dto.Correo
                .Trim()
                .ToLowerInvariant();

            var correoExiste = await _context.Usuarios
                .AnyAsync(u =>
                    u.Correo == correoNormalizado &&
                    u.Id != id
                );

            if (correoExiste)
            {
                return BadRequest("Ya existe otro usuario con ese correo." );
            }

            usuario.Nombre = dto.Nombre.Trim();
            usuario.Correo = correoNormalizado;

            await _context.SaveChangesAsync();

            return NoContent();
        }

     
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .FindAsync(id);

            if (usuario == null)
            {
                return NotFound("El usuario no existe.");
            }

            var idUsuarioAutenticadoTexto =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(
                    idUsuarioAutenticadoTexto,
                    out var idUsuarioAutenticado) &&
                idUsuarioAutenticado == id)
            {
                return BadRequest("No puede eliminar el usuario con el que inició sesión." );
            }

            var cantidadAdministradores = await _context.Usuarios.CountAsync(u => u.Rol == Roles.Administrador);
            if (usuario.Rol == Roles.Administrador && cantidadAdministradores <= 1)
                return BadRequest("No se puede eliminar el último administrador.");

            var cantidadUsuarios = await _context.Usuarios
                .CountAsync();

            if (cantidadUsuarios <= 1)
            {
                return BadRequest( "No se puede eliminar el último usuario del sistema.");
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}