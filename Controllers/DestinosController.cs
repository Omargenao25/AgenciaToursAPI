using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DestinosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DestinosController(ApplicationDbContext context)
        {
            _context = context;
        }


      
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Destino>>> GetDestinos()
        {
            return await _context.Destinos
                .Include(d => d.Pais)
                .ToListAsync();
        }



        [HttpGet("{id}")]
        public async Task<ActionResult<Destino>> GetDestino(int id)
        {
            var destino = await _context.Destinos
                .Include(d => d.Pais)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (destino == null)
            {
                return NotFound();
            }

            return destino;
        }



        [HttpPost]
        public async Task<ActionResult<Destino>> PostDestino(Destino destino)
        {
            var pais = await _context.Paises.FindAsync(destino.PaisId);

            if (pais == null)
            {
                return NotFound("El país especificado no existe.");
            }

            _context.Destinos.Add(destino);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetDestino),
                new { id = destino.Id },
                destino
            );
        }



        [HttpPut("{id}")]
        public async Task<IActionResult> PutDestino(int id, Destino destino)
        {
            if (id != destino.Id)
            {
                return BadRequest("El Id de la URL no coincide con el Id del destino.");
            }

            var paisExiste = await _context.Paises.AnyAsync(
                p => p.Id == destino.PaisId
            );

            if (!paisExiste)
            {
                return NotFound("El país especificado no existe.");
            }

            _context.Entry(destino).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DestinoExiste(id))
                {
                    return NotFound("El destino no existe.");
                }

                throw;
            }

            return NoContent();
        }


        private bool DestinoExiste(int id)
        {
            return _context.Destinos.Any(e => e.Id == id);
        }


   
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDestino(int id)
        {
            var destino = await _context.Destinos.FindAsync(id);

            if (destino == null)
            {
                return NotFound("El destino no existe.");
            }

            var tieneTours = await _context.Tours
                .AnyAsync(t => t.DestinoId == id);

            if (tieneTours)
            {
                return BadRequest( "No se puede eliminar el destino porque tiene tours asociados.");
            }

            _context.Destinos.Remove(destino);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}