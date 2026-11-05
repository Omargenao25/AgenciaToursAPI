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
    public class CategoriasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Categoria>>> GetCategorias()
        {
            var categorias = await _context.Categorias
                .AsNoTracking()
                .ToListAsync();

            return Ok(categorias);
        }


        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<Categoria>> GetCategoria(int id)
        {
            var categoria = await _context.Categorias
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria == null)
            {
                return NotFound("La categoría no existe.");
            }

            return Ok(categoria);
        }

        
        [HttpPost]
        public async Task<ActionResult<Categoria>> PostCategoria(
            Categoria categoria)
        {
            categoria.Nombre = categoria.Nombre.Trim();

            var existe = await _context.Categorias
                .AnyAsync(c =>
                    c.Nombre.ToLower() == categoria.Nombre.ToLower()
                );

            if (existe)
            {
                return BadRequest("Ya existe una categoría con ese nombre." );
            }

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCategoria),
                new { id = categoria.Id },
                categoria
            );
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategoria(
            int id,
            Categoria categoria)
        {
            if (id != categoria.Id)
            {
                return BadRequest("El Id de la URL no coincide con el Id de la categoría.");
            }

            var categoriaActual = await _context.Categorias
                .FindAsync(id);

            if (categoriaActual == null)
            {
                return NotFound("La categoría no existe.");
            }

            categoria.Nombre = categoria.Nombre.Trim();

            var existe = await _context.Categorias
                .AnyAsync(c =>
                    c.Nombre.ToLower() == categoria.Nombre.ToLower() &&
                    c.Id != id
                );

            if (existe)
            {
                return BadRequest("Ya existe otra categoría con ese nombre.");
            }

            categoriaActual.Nombre = categoria.Nombre;

            await _context.SaveChangesAsync();

            return NoContent();
        }

  
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategoria(int id)
        {
            var categoria = await _context.Categorias
                .FindAsync(id);

            if (categoria == null)
            {
                return NotFound("La categoría no existe.");
            }

            var tieneTours = await _context.Tours
                .AnyAsync(t => t.CategoriaId == id);

            if (tieneTours)
            {
                return BadRequest("No se puede eliminar la categoría porque tiene tours asociados." );
            }

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}