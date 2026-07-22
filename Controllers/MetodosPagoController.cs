using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetodosPagoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MetodosPagoController(ApplicationDbContext context)
        {
            _context = context;
        }


     
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MetodoPago>>> GetMetodosPago()
        {
            return await _context.MetodosPago.ToListAsync();
        }


        
        [HttpGet("{id}")]
        public async Task<ActionResult<MetodoPago>> GetMetodoPago(int id)
        {
            var metodoPago = await _context.MetodosPago.FindAsync(id);

            if (metodoPago == null)
            {
                return NotFound("El método de pago no existe.");
            }

            return metodoPago;
        }


        [HttpPost]
        public async Task<ActionResult<MetodoPago>> PostMetodoPago(MetodoPago metodoPago)
        {
            _context.MetodosPago.Add(metodoPago);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMetodoPago),
                new { id = metodoPago.Id },
                metodoPago
            );
        }


      
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMetodoPago(int id, MetodoPago metodoPago)
        {
            if (id != metodoPago.Id)
            {
                return BadRequest("El Id no coincide.");
            }

            _context.Entry(metodoPago).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MetodoPagoExiste(id))
                {
                    return NotFound("El método de pago no existe.");
                }

                throw;
            }

            return NoContent();
        }

        private bool MetodoPagoExiste(int id)
        {
            return _context.MetodosPago.Any(m => m.Id == id);
        }




        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMetodoPago(int id)
        {
            var metodoPago = await _context.MetodosPago.FindAsync(id);

            if (metodoPago == null)
            {
                return NotFound("El método de pago no existe.");
            }

            var tieneReservas = await _context.Reservas
                .AnyAsync(r => r.MetodoPagoId == id);

            if (tieneReservas)
            {
                return BadRequest(
                    "No se puede eliminar el método de pago porque tiene reservas asociadas."
                );
            }

            _context.MetodosPago.Remove(metodoPago);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}