using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransportesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TransportesController(ApplicationDbContext context)
        {
            _context = context;
        }


      
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transporte>>> GetTransportes()
        {
            return await _context.Transportes.ToListAsync();
        }


     
        [HttpGet("{id}")]
        public async Task<ActionResult<Transporte>> GetTransporte(int id)
        {
            var transporte = await _context.Transportes.FindAsync(id);

            if (transporte == null)
            {
                return NotFound("El transporte no existe.");
            }

            return transporte;
        }


        [HttpPost]
        public async Task<ActionResult<Transporte>> PostTransporte(Transporte transporte)
        {
            _context.Transportes.Add(transporte);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetTransporte),
                new { id = transporte.Id },
                transporte
            );
        }


     
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTransporte(int id, Transporte transporte)
        {
            if (id != transporte.Id)
            {
                return BadRequest("El Id no coincide.");
            }

            _context.Entry(transporte).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TransporteExiste(id))
                {
                    return NotFound("El transporte no existe.");
                }

                throw;
            }

            return NoContent();
        }

        private bool TransporteExiste(int id)
        {
            return _context.Transportes.Any(t => t.Id == id);
        }


  
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransporte(int id)
        {
            var transporte = await _context.Transportes.FindAsync(id);

            if (transporte == null)
            {
                return NotFound("El transporte no existe.");
            }

            var tieneTours = await _context.Tours
                .AnyAsync(t => t.TransporteId == id);

            if (tieneTours)
            {
                return BadRequest(
                    "No se puede eliminar el transporte porque tiene tours asociados."
                );
            }

            _context.Transportes.Remove(transporte);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}