using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaisesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PaisesController(ApplicationDbContext context)
        {
            _context = context;
        }


      
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pais>>> GetPaises()
        {
            return await _context.Paises.ToListAsync();
        }


    
        [HttpPost]
        public async Task<ActionResult<Pais>> PostPais(Pais pais)
        {
            _context.Paises.Add(pais);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetPaises),
                new { id = pais.Id },
                pais
            );
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Pais>> GetPais(int id)
        {
            var pais = await _context.Paises.FindAsync(id);

            if (pais == null)
            {
                return NotFound();
            }

            return pais;
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> PutPais(int id, Pais pais)
        {
            if (id != pais.Id)
            {
                return BadRequest();
            }

            _context.Entry(pais).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaisExiste(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        private bool PaisExiste(int id)
        {
            return _context.Paises.Any(p => p.Id == id);
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePais(int id)
        {
            var pais = await _context.Paises.FindAsync(id);

            if (pais == null)
            {
                return NotFound();
            }

            _context.Paises.Remove(pais);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}