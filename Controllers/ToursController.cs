using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToursController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ToursController(ApplicationDbContext context)
        {
            _context = context;
        }



        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tour>>> GetTours()
        {
            return await _context.Tours
                .Include(t => t.Pais)
                .Include(t => t.Destino)
                .Include(t => t.Categoria)
                .Include(t => t.Guia)
                .Include(t => t.Transporte)
                .ToListAsync();
        }


   
        [HttpGet("{id}")]
        public async Task<ActionResult<Tour>> GetTour(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.Pais)
                .Include(t => t.Destino)
                .Include(t => t.Categoria)
                .Include(t => t.Guia)
                .Include(t => t.Transporte)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                return NotFound("El tour no existe.");
            }

            return tour;
        }


      
        [HttpPost]
        public async Task<ActionResult<Tour>> PostTour(Tour tour)
        {
            var paisExiste = await _context.Paises
                .AnyAsync(p => p.Id == tour.PaisId);

            if (!paisExiste)
            {
                return NotFound("El país especificado no existe.");
            }

            var destino = await _context.Destinos
                .FirstOrDefaultAsync(d => d.Id == tour.DestinoId);

            if (destino == null)
            {
                return NotFound("El destino especificado no existe.");
            }

            if (destino.PaisId != tour.PaisId)
            {
                return BadRequest(
                    "El destino seleccionado no pertenece al país indicado."
                );
            }

            var categoriaExiste = await _context.Categorias
                .AnyAsync(c => c.Id == tour.CategoriaId);

            if (!categoriaExiste)
            {
                return NotFound("La categoría especificada no existe.");
            }

            var guiaExiste = await _context.Guias
                .AnyAsync(g => g.Id == tour.GuiaId);

            if (!guiaExiste)
            {
                return NotFound("El guía especificado no existe.");
            }

            var transporteExiste = await _context.Transportes
                .AnyAsync(t => t.Id == tour.TransporteId);

            if (!transporteExiste)
            {
                return NotFound("El transporte especificado no existe.");
            }

            tour.Itbis = tour.Precio * 0.18m;

            tour.DuracionDias = destino.DuracionDias;
            tour.DuracionHoras = destino.DuracionHoras;

            var fechaInicio = tour.Fecha.Date.Add(tour.Hora);

            tour.FechaFin = fechaInicio
                .AddDays(tour.DuracionDias)
                .AddHours(tour.DuracionHoras);

            if (string.IsNullOrWhiteSpace(tour.Estado))
            {
                tour.Estado = "Disponible";
            }

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetTour),
                new { id = tour.Id },
                tour
            );
        }



        [HttpPut("{id}")]
        public async Task<IActionResult> PutTour(int id, Tour tour)
        {
            if (id != tour.Id)
            {
                return BadRequest(
                    "El Id de la URL no coincide con el Id del tour."
                );
            }

            var tourExiste = await _context.Tours
                .AnyAsync(t => t.Id == id);

            if (!tourExiste)
            {
                return NotFound("El tour no existe.");
            }

            var paisExiste = await _context.Paises
                .AnyAsync(p => p.Id == tour.PaisId);

            if (!paisExiste)
            {
                return NotFound("El país especificado no existe.");
            }

            var destino = await _context.Destinos
                .FirstOrDefaultAsync(d => d.Id == tour.DestinoId);

            if (destino == null)
            {
                return NotFound("El destino especificado no existe.");
            }

            if (destino.PaisId != tour.PaisId)
            {
                return BadRequest(
                    "El destino seleccionado no pertenece al país indicado."
                );
            }

            var categoriaExiste = await _context.Categorias
                .AnyAsync(c => c.Id == tour.CategoriaId);

            if (!categoriaExiste)
            {
                return NotFound("La categoría especificada no existe.");
            }

            var guiaExiste = await _context.Guias
                .AnyAsync(g => g.Id == tour.GuiaId);

            if (!guiaExiste)
            {
                return NotFound("El guía especificado no existe.");
            }

            var transporteExiste = await _context.Transportes
                .AnyAsync(t => t.Id == tour.TransporteId);

            if (!transporteExiste)
            {
                return NotFound("El transporte especificado no existe.");
            }

            tour.Itbis = tour.Precio * 0.18m;

            tour.DuracionDias = destino.DuracionDias;
            tour.DuracionHoras = destino.DuracionHoras;

            var fechaInicio = tour.Fecha.Date.Add(tour.Hora);

            tour.FechaFin = fechaInicio
                .AddDays(tour.DuracionDias)
                .AddHours(tour.DuracionHoras);

            if (string.IsNullOrWhiteSpace(tour.Estado))
            {
                tour.Estado = "Disponible";
            }

            _context.Entry(tour).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound("El tour ya no existe.");
            }

            return NoContent();
        }


        

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var tour = await _context.Tours.FindAsync(id);

            if (tour == null)
            {
                return NotFound("El tour no existe.");
            }

            var tieneReservas = await _context.Reservas
                .AnyAsync(r => r.TourId == id);

            if (tieneReservas)
            {
                return BadRequest(
                    "No se puede eliminar el tour porque tiene reservas asociadas."
                );
            }

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}