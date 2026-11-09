using Microsoft.Data.SqlClient;
using AgenciaToursAPI.Security;
using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers
{
    [Authorize(Roles = Roles.Personal)]
    [Route("api/[controller]")]
    [ApiController]
    public class PaisesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PaisesController(ApplicationDbContext context)
        {
            _context = context;
        }


 
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pais>>> GetPaises()
        {
            var paises = await _context.Paises
                .AsNoTracking()
                .ToListAsync();

            return Ok(paises);
        }


     
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<Pais>> GetPais(int id)
        {
            var pais = await _context.Paises
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pais == null)
            {
                return NotFound("El país no existe.");
            }

            return Ok(pais);
        }


        [HttpPost]
        public async Task<ActionResult<Pais>> PostPais(Pais pais)
        {
            pais.Nombre = pais.Nombre?.Trim() ?? string.Empty;
            if (pais.Nombre.Length < 2)
                return BadRequest("El nombre del país debe contener al menos dos caracteres.");


            var existe = await _context.Paises
                .AnyAsync(p =>
                    p.Nombre.ToLower() == pais.Nombre.ToLower()
                );

            if (existe)
            {
                return Conflict("Ya existe un país con ese nombre.");
            }


            _context.Paises.Add(pais);

try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
            {
                return Conflict("Ya existe un país con esos datos.");
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number == 547)
            {
                return Conflict("No se puede realizar la operación porque existen registros relacionados.");
            }


            return CreatedAtAction(
                nameof(GetPais),
                new { id = pais.Id },
                pais
            );
        }


        
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPais(
            int id,
            Pais pais)
        {
            if (id != pais.Id)
            {
                return BadRequest( "El Id de la URL no coincide con el Id del país.");
            }


            var paisActual = await _context.Paises
                .FindAsync(id);


            if (paisActual == null)
            {
                return NotFound( "El país no existe." );
            }


            pais.Nombre = pais.Nombre?.Trim() ?? string.Empty;
            if (pais.Nombre.Length < 2)
                return BadRequest("El nombre del país debe contener al menos dos caracteres.");


            var existe = await _context.Paises
                .AnyAsync(p =>
                    p.Nombre.ToLower() == pais.Nombre.ToLower()
                    &&
                    p.Id != id
                );


            if (existe)
            {
                return Conflict("Ya existe otro país con ese nombre.");
            }


            paisActual.Nombre = pais.Nombre;


try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
            {
                return Conflict("Ya existe un país con esos datos.");
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number == 547)
            {
                return Conflict("No se puede realizar la operación porque existen registros relacionados.");
            }


            return NoContent();
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePais(int id)
        {
            var pais = await _context.Paises
                .FindAsync(id);


            if (pais == null)
            {
                return NotFound("El país no existe.");
            }


            var tieneDestinos = await _context.Destinos
                .AnyAsync(d => d.PaisId == id);


            var tieneTours = await _context.Tours
                .AnyAsync(t => t.PaisId == id);



            if (tieneDestinos || tieneTours)
            {
                return Conflict("No se puede eliminar el país porque tiene destinos o tours asociados.");
            }


            _context.Paises.Remove(pais);

try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
            {
                return Conflict("Ya existe un país con esos datos.");
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number == 547)
            {
                return Conflict("No se puede realizar la operación porque existen registros relacionados.");
            }


            return NoContent();
        }
    }
}