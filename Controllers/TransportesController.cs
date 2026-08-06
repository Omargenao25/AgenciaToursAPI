using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers
{
    [Authorize]
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
            var transportes = await _context.Transportes
                .AsNoTracking()
                .ToListAsync();

            return Ok(transportes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Transporte>> GetTransporte(int id)
        {
            var transporte = await _context.Transportes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transporte == null)
            {
                return NotFound("El transporte no existe.");
            }

            return Ok(transporte);
        }

        [HttpPost]
        public async Task<ActionResult<Transporte>> PostTransporte(
            Transporte transporte)
        {
            NormalizarTransporte(transporte);

            var existe = await _context.Transportes
                .AnyAsync(t =>
                    t.Tipo.ToLower() == transporte.Tipo.ToLower() &&
                    t.Descripcion.ToLower() == transporte.Descripcion.ToLower()
                );

            if (existe)
            {
                return BadRequest(
                    "Ya existe un transporte con el mismo tipo y descripción."
                );
            }

            _context.Transportes.Add(transporte);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetTransporte),
                new { id = transporte.Id },
                transporte
            );
        }

        
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTransporte(
            int id,
            Transporte transporte)
        {
            if (id != transporte.Id)
            {
                return BadRequest(
                    "El Id de la URL no coincide con el Id del transporte."
                );
            }

            var transporteActual = await _context.Transportes
                .FindAsync(id);

            if (transporteActual == null)
            {
                return NotFound("El transporte no existe.");
            }

            NormalizarTransporte(transporte);

            var existe = await _context.Transportes
                .AnyAsync(t =>
                    t.Tipo.ToLower() == transporte.Tipo.ToLower() &&
                    t.Descripcion.ToLower() == transporte.Descripcion.ToLower() &&
                    t.Id != id
                );

            if (existe)
            {
                return BadRequest(
                    "Ya existe otro transporte con el mismo tipo y descripción."
                );
            }

            transporteActual.Tipo = transporte.Tipo;
            transporteActual.Descripcion = transporte.Descripcion;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransporte(int id)
        {
            var transporte = await _context.Transportes
                .FindAsync(id);

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

        private static void NormalizarTransporte(Transporte transporte)
        {
            transporte.Tipo = transporte.Tipo.Trim();
            transporte.Descripcion = transporte.Descripcion.Trim();
        }
    }
}