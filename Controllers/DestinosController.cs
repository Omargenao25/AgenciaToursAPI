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
    public class DestinosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DestinosController(ApplicationDbContext context)
        {
            _context = context;
        }


        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Destino>>> GetDestinos()
        {
            var destinos = await _context.Destinos
                .AsNoTracking()
                .Include(d => d.Pais)
                .ToListAsync();

            return Ok(destinos);
        }



        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<Destino>> GetDestino(int id)
        {
            var destino = await _context.Destinos
                .AsNoTracking()
                .Include(d => d.Pais)
                .FirstOrDefaultAsync(d => d.Id == id);


            if (destino == null)
            {
                return NotFound( "El destino no existe.");
            }


            return Ok(destino);
        }



        [HttpPost]
        public async Task<ActionResult<Destino>> PostDestino(
            Destino destino)
        {

            destino.Nombre = destino.Nombre.Trim();


            var paisExiste = await _context.Paises
                .AnyAsync(p => p.Id == destino.PaisId);


            if (!paisExiste)
            {
                return NotFound("El país especificado no existe.");
            }


            var existe = await _context.Destinos
                .AnyAsync(d =>
                    d.Nombre.ToLower() == destino.Nombre.ToLower()
                    &&
                    d.PaisId == destino.PaisId
                );


            if (existe)
            {
                return BadRequest("Ya existe este destino para el país seleccionado.");
            }


            if (destino.DuracionDias == 0 &&
                destino.DuracionHoras == 0)
            {
                return BadRequest("La duración total del destino debe ser mayor que cero.");
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
        public async Task<IActionResult> PutDestino(
            int id,
            Destino destino)
        {

            if (id != destino.Id)
            {
                return BadRequest("El Id de la URL no coincide con el Id del destino.");
            }


            var destinoActual = await _context.Destinos
                .FindAsync(id);


            if (destinoActual == null)
            {
                return NotFound("El destino no existe.");
            }


            var paisExiste = await _context.Paises
                .AnyAsync(p => p.Id == destino.PaisId);


            if (!paisExiste)
            {
                return NotFound("El país especificado no existe." );
            }


            destino.Nombre = destino.Nombre.Trim();


            var existe = await _context.Destinos
                .AnyAsync(d =>
                    d.Nombre.ToLower() == destino.Nombre.ToLower()
                    &&
                    d.PaisId == destino.PaisId
                    &&
                    d.Id != id
                );


            if (existe)
            {
                return BadRequest("Ya existe otro destino igual para ese país.");
            }

            if (destino.DuracionDias == 0 &&
                destino.DuracionHoras == 0)

            {
                return BadRequest("La duración total del destino debe ser mayor que cero." );
            }


            var tieneTours = await _context.Tours
                .AnyAsync(t => t.DestinoId == id);


            if (tieneTours &&
                destinoActual.PaisId != destino.PaisId)
            {
                return BadRequest("No se puede cambiar el país de un destino que tiene tours asociados.");
            }



            destinoActual.Nombre = destino.Nombre;
            destinoActual.DuracionDias = destino.DuracionDias;
            destinoActual.DuracionHoras = destino.DuracionHoras;
            destinoActual.PaisId = destino.PaisId;


            await _context.SaveChangesAsync();


            return NoContent();
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDestino(int id)
        {
            var destino = await _context.Destinos
                .FindAsync(id);


            if (destino == null)
            {
                return NotFound( "El destino no existe." );
            }


            var tieneTours = await _context.Tours
                .AnyAsync(t => t.DestinoId == id);


            if (tieneTours)
            {
                return BadRequest("No se puede eliminar el destino porque tiene tours asociados.");
            }


            _context.Destinos.Remove(destino);

            await _context.SaveChangesAsync();


            return NoContent();
        }
    }
}