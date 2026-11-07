using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Models;
using AgenciaToursAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AgenciaToursAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthController(
            ApplicationDbContext context,
            IPasswordHasher<Usuario> passwordHasher,
            IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

    
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginRespuestaDto>> Login(
            LoginDto dto)
        {
            var correoNormalizado = dto.Correo
                .Trim()
                .ToLowerInvariant();

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(
                    u => u.Correo == correoNormalizado
                );

            if (usuario == null)
            {
                return Unauthorized(new
                {
                    mensaje = "Correo o contraseña incorrectos."
                });
            }

            var resultado = _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.PasswordHash,
                dto.Contraseña
            );

            if (resultado == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    mensaje = "Correo o contraseña incorrectos."
                });
            }

           
            if (resultado ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.PasswordHash =
                    _passwordHasher.HashPassword(
                        usuario,
                        dto.Contraseña
                    );

                await _context.SaveChangesAsync();
            }

            var minutosExpiracion =
                _configuration.GetValue<int>(
                    "Jwt:ExpireMinutes"
                );

            if (minutosExpiracion <= 0)
            {
                throw new InvalidOperationException(
                    "El tiempo de expiración del JWT no está configurado correctamente."
                );
            }

            var expiracion = DateTime.UtcNow
                .AddMinutes(minutosExpiracion);

            var token = GenerarToken(
                usuario,
                expiracion
            );

            var respuesta = new LoginRespuestaDto
            {
                Token = token,
                Expiracion = expiracion,
                Nombre = usuario.Nombre,
                Rol = usuario.Rol
            };

            return Ok(respuesta);
        }

        private string GenerarToken(
            Usuario usuario,
            DateTime expiracion)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "La clave JWT no está configurada."
                );

            var jwtIssuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "El emisor JWT no está configurado."
                );

            var jwtAudience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "La audiencia JWT no está configurada."
                );

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.Id.ToString()
                ),

                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    usuario.Id.ToString()
                ),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    usuario.Correo
                ),

                new Claim(
                    ClaimTypes.Name,
                    usuario.Nombre
                ),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("version_sesion", usuario.VersionSesion),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()
                )
            };

            var clave = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );

            var credenciales = new SigningCredentials(
                clave,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiracion,
                signingCredentials: credenciales
            );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}