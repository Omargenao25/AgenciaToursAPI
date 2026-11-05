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
            var guias = await _context.Guias
                .AsNoTracking()
                .ToListAsync();

            return Ok(guias);
        }

      
        [HttpGet("{id}")]
        public async Task<ActionResult<Guia>> GetGuia(int id)
        {
            var guia = await _context.Guias
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);

            if (guia == null)
            {
                return NotFound("El guía no existe.");
            }

            return Ok(guia);
        }

        
        [HttpPost]
        public async Task<ActionResult<Guia>> PostGuia(Guia guia)
        {
            NormalizarGuia(guia);

            var telefonoExiste = await _context.Guias
                .AnyAsync(g => g.Telefono == guia.Telefono);

            if (telefonoExiste)
            {
                return BadRequest("Ya existe un guía con ese número de teléfono.");
            }

            _context.Guias.Add(guia);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetGuia),
                new { id = guia.Id },
                guia
            );
        }

        
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGuia(
            int id,
            Guia guia)
        {
            if (id != guia.Id)
            {
                return BadRequest("El Id de la URL no coincide con el Id del guía.");
            }

            var guiaActual = await _context.Guias
                .FindAsync(id);

            if (guiaActual == null)
            {
                return NotFound("El guía no existe.");
            }

            NormalizarGuia(guia);

            var telefonoExiste = await _context.Guias
                .AnyAsync(g =>
                    g.Telefono == guia.Telefono &&
                    g.Id != id
                );

            if (telefonoExiste)
            {
                return BadRequest("Ya existe otro guía con ese número de teléfono.");
            }

            guiaActual.Nombre = guia.Nombre;
            guiaActual.Telefono = guia.Telefono;

            await _context.SaveChangesAsync();

            return NoContent();
        }

       
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGuia(int id)
        {
            var guia = await _context.Guias
                .FindAsync(id);

            if (guia == null)
            {
                return NotFound("El guía no existe.");
            }

            var tieneTours = await _context.Tours
                .AnyAsync(t => t.GuiaId == id);

            if (tieneTours)
            {
                return BadRequest( "No se puede eliminar el guía porque tiene tours asociados.");
            }

            _context.Guias.Remove(guia);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static void NormalizarGuia(Guia guia)
        {
            guia.Nombre = guia.Nombre.Trim();
            guia.Telefono = guia.Telefono.Trim();
        }
    }
}