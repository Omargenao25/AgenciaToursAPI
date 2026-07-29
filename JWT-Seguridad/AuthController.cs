using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Models;
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

       
        [HttpPost("login")]
        public async Task<ActionResult<LoginRespuestaDto>> Login(
            LoginDto dto)
        {
            var correoNormalizado = dto.Correo
                .Trim()
                .ToLower();

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(
                    u => u.Correo == correoNormalizado
                );

            if (usuario == null)
            {
                return Unauthorized("Correo o contraseña incorrectos.");
            }

            var resultado = _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.PasswordHash,
                dto.Contraseña
            );

            if (resultado == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Correo o contraseña incorrectos.");
            }

            var expiracion = DateTime.UtcNow.AddMinutes(
                _configuration.GetValue<int>("Jwt:ExpireMinutes")
            );

            var token = GenerarToken(usuario, expiracion);

            var respuesta = new LoginRespuestaDto
            {
                Token = token,
                Expiracion = expiracion,
                Nombre = usuario.Nombre
            };

            return Ok(respuesta);
        }

        private string GenerarToken(
            Usuario usuario,
            DateTime expiracion)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("La clave JWT no está configurada.");

            var claims = new List<Claim>
            {
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
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiracion,
                signingCredentials: credenciales
            );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}