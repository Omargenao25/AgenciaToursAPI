using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgenciaToursAPI.Controllers
{
    [Authorize]
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
                    Correo = u.Correo
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
                    Correo = u.Correo
                })
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                return NotFound("El usuario no existe.");
            }

            return Ok(usuario);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<UsuarioRespuestaDto>>
            PostUsuario(CrearUsuarioDto dto)
        {
            var correoNormalizado = dto.Correo
                .Trim()
                .ToLowerInvariant();

            var correoExiste = await _context.Usuarios
                .AnyAsync(u => u.Correo == correoNormalizado);

            if (correoExiste)
            {
                return BadRequest( "Ya existe un usuario con ese correo.");
            }

            var usuario = new Usuario
            {
                Nombre = dto.Nombre.Trim(),
                Correo = correoNormalizado
            };

            usuario.PasswordHash = _passwordHasher.HashPassword(
                usuario,
                dto.Contraseña
            );

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var respuesta = new UsuarioRespuestaDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo
            };

            return CreatedAtAction(
                nameof(GetUsuario),
                new { id = usuario.Id },
                respuesta
            );
        }


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