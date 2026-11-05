using AgenciaToursAPI.Security;
﻿using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers
{
    [Authorize(Roles = Roles.Personal)]
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
            var metodosPago = await _context.MetodosPago
                .AsNoTracking()
                .ToListAsync();

            return Ok(metodosPago);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<MetodoPago>> GetMetodoPago(int id)
        {
            var metodoPago = await _context.MetodosPago
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (metodoPago == null)
            {
                return NotFound("El método de pago no existe.");
            }

            return Ok(metodoPago);
        }

      
        [HttpPost]
        public async Task<ActionResult<MetodoPago>> PostMetodoPago(
            MetodoPago metodoPago)
        {
            metodoPago.Nombre = metodoPago.Nombre.Trim();

            var existe = await _context.MetodosPago
                .AnyAsync(m =>
                    m.Nombre.ToLower() == metodoPago.Nombre.ToLower()
                );

            if (existe)
            {
                return BadRequest(
                    "Ya existe un método de pago con ese nombre."
                );
            }

            _context.MetodosPago.Add(metodoPago);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMetodoPago),
                new { id = metodoPago.Id },
                metodoPago
            );
        }

        
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMetodoPago(
            int id,
            MetodoPago metodoPago)
        {
            if (id != metodoPago.Id)
            {
                return BadRequest(
                    "El Id de la URL no coincide con el Id del método de pago."
                );
            }

            var metodoPagoActual = await _context.MetodosPago
                .FindAsync(id);

            if (metodoPagoActual == null)
            {
                return NotFound(
                    "El método de pago no existe."
                );
            }

            metodoPago.Nombre = metodoPago.Nombre.Trim();

            var existe = await _context.MetodosPago
                .AnyAsync(m =>
                    m.Nombre.ToLower() == metodoPago.Nombre.ToLower() &&
                    m.Id != id
                );

            if (existe)
            {
                return BadRequest(
                    "Ya existe otro método de pago con ese nombre."
                );
            }

            metodoPagoActual.Nombre = metodoPago.Nombre;

            await _context.SaveChangesAsync();

            return NoContent();
        }

       
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMetodoPago(int id)
        {
            var metodoPago = await _context.MetodosPago
                .FindAsync(id);

            if (metodoPago == null)
            {
                return NotFound(
                    "El método de pago no existe."
                );
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