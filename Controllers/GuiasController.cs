using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GuiasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GuiasController(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Guia>>> GetGuias()
        {
            return await _context.Guias.ToListAsync();
        }


      
        [HttpGet("{id}")]
        public async Task<ActionResult<Guia>> GetGuia(int id)
        {
            var guia = await _context.Guias.FindAsync(id);

            if (guia == null)
            {
                return NotFound("El guía no existe.");
            }

            return guia;
        }


       
        [HttpPost]
        public async Task<ActionResult<Guia>> PostGuia(Guia guia)
        {
            _context.Guias.Add(guia);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetGuia),
                new { id = guia.Id },
                guia
            );
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> PutGuia(int id, Guia guia)
        {
            if (id != guia.Id)
            {
                return BadRequest("El Id no coincide.");
            }

            _context.Entry(guia).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GuiaExiste(id))
                {
                    return NotFound("El guía no existe.");
                }

                throw;
            }

            return NoContent();
        }
        private bool GuiaExiste(int id)
        {
            return _context.Guias.Any(g => g.Id == id);
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGuia(int id)
        {
            var guia = await _context.Guias.FindAsync(id);

            if (guia == null)
            {
                return NotFound("El guía no existe.");
            }

            var tieneTours = await _context.Tours
                .AnyAsync(t => t.GuiaId == id);

            if (tieneTours)
            {
                return BadRequest(
                    "No se puede eliminar el guía porque tiene tours asociados."
                );
            }

            _context.Guias.Remove(guia);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}