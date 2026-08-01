using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AgenciaToursAPI.Controllers
{
    [Authorize]
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
        public async Task<ActionResult<IEnumerable<TourRespuestaDto>>> GetTours()
        {
            var tours = await _context.Tours
                .Select(t => new TourRespuestaDto
                {
                    Id = t.Id,
                    Nombre = t.Nombre,

                    Pais = t.Pais != null
                        ? t.Pais.Nombre
                        : string.Empty,

                    Destino = t.Destino != null
                        ? t.Destino.Nombre
                        : string.Empty,

                    Categoria = t.Categoria != null
                        ? t.Categoria.Nombre
                        : string.Empty,

                    Guia = t.Guia != null
                        ? t.Guia.Nombre
                        : string.Empty,

                    Transporte = t.Transporte != null
                        ? t.Transporte.Tipo
                        : string.Empty,

                    Fecha = t.Fecha,
                    Hora = t.Hora,
                    Precio = t.Precio,
                    Itbis = t.Itbis,
                    DuracionDias = t.DuracionDias,
                    DuracionHoras = t.DuracionHoras,
                    FechaFin = t.FechaFin,
                    Estado = t.Estado
                })
                .ToListAsync();

            foreach (var tour in tours)
            {
                tour.Estado = CalcularEstado(
                    tour.Fecha,
                    tour.Hora,
                    tour.FechaFin
                );
            }

            return Ok(tours);
        }

   

        [HttpGet("{id}")]
        public async Task<ActionResult<TourRespuestaDto>> GetTour(int id)
        {
            var tour = await _context.Tours
                .Where(t => t.Id == id)
                .Select(t => new TourRespuestaDto
                {
                    Id = t.Id,
                    Nombre = t.Nombre,

                    Pais = t.Pais != null
                        ? t.Pais.Nombre
                        : string.Empty,

                    Destino = t.Destino != null
                        ? t.Destino.Nombre
                        : string.Empty,

                    Categoria = t.Categoria != null
                        ? t.Categoria.Nombre
                        : string.Empty,

                    Guia = t.Guia != null
                        ? t.Guia.Nombre
                        : string.Empty,

                    Transporte = t.Transporte != null
                        ? t.Transporte.Tipo
                        : string.Empty,

                    Fecha = t.Fecha,
                    Hora = t.Hora,
                    Precio = t.Precio,
                    Itbis = t.Itbis,
                    DuracionDias = t.DuracionDias,
                    DuracionHoras = t.DuracionHoras,
                    FechaFin = t.FechaFin,
                    Estado = t.Estado
                })
                .FirstOrDefaultAsync();

            if (tour == null)
            {
                return NotFound("El tour no existe.");
            }

            tour.Estado = CalcularEstado(
                tour.Fecha,
                tour.Hora,
                tour.FechaFin
            );

            return Ok(tour);
        }

      

        [HttpPost]
        public async Task<ActionResult<TourRespuestaDto>> PostTour(
            CrearTourDto dto)
        {
            var paisExiste = await _context.Paises
                .AnyAsync(p => p.Id == dto.PaisId);

            if (!paisExiste)
            {
                return NotFound(
                    "El país especificado no existe."
                );
            }

            var destino = await _context.Destinos
                .FirstOrDefaultAsync(d => d.Id == dto.DestinoId);

            if (destino == null)
            {
                return NotFound(
                    "El destino especificado no existe."
                );
            }

            if (destino.PaisId != dto.PaisId)
            {
                return BadRequest(
                    "El destino seleccionado no pertenece al país indicado."
                );
            }

            var categoriaExiste = await _context.Categorias
                .AnyAsync(c => c.Id == dto.CategoriaId);

            if (!categoriaExiste)
            {
                return NotFound(
                    "La categoría especificada no existe."
                );
            }

            var guiaExiste = await _context.Guias
                .AnyAsync(g => g.Id == dto.GuiaId);

            if (!guiaExiste)
            {
                return NotFound(
                    "El guía especificado no existe."
                );
            }

            var transporteExiste = await _context.Transportes
                .AnyAsync(t => t.Id == dto.TransporteId);

            if (!transporteExiste)
            {
                return NotFound(
                    "El transporte especificado no existe."
                );
            }

            var tour = new Tour
            {
                Nombre = dto.Nombre,
                PaisId = dto.PaisId,
                DestinoId = dto.DestinoId,
                CategoriaId = dto.CategoriaId,
                GuiaId = dto.GuiaId,
                TransporteId = dto.TransporteId,
                Fecha = dto.Fecha,
                Hora = dto.Hora,
                Precio = dto.Precio
            };

            PrepararDatosCalculados(tour, destino);

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            var respuesta = await ObtenerTourRespuesta(tour.Id);

            if (respuesta == null)
            {
                return StatusCode(
                    500,
                    "El tour fue creado, pero no se pudo generar la respuesta."
                );
            }

            return CreatedAtAction(
                nameof(GetTour),
                new { id = tour.Id },
                respuesta
            );
        }

       

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTour(
            int id,ActualizarTourDto dto)
        {
            var tour = await _context.Tours.FindAsync(id);

            if (tour == null)
            {
                return NotFound("El tour no existe.");
            }

            var paisExiste = await _context.Paises
                .AnyAsync(p => p.Id == dto.PaisId);

            if (!paisExiste)
            {
                return NotFound(
                    "El país especificado no existe."
                );
            }

            var destino = await _context.Destinos
                .FirstOrDefaultAsync(d => d.Id == dto.DestinoId);

            if (destino == null)
            {
                return NotFound(
                    "El destino especificado no existe."
                );
            }

            if (destino.PaisId != dto.PaisId)
            {
                return BadRequest(
                    "El destino seleccionado no pertenece al país indicado."
                );
            }

            var categoriaExiste = await _context.Categorias
                .AnyAsync(c => c.Id == dto.CategoriaId);

            if (!categoriaExiste)
            {
                return NotFound(
                    "La categoría especificada no existe."
                );
            }

            var guiaExiste = await _context.Guias
                .AnyAsync(g => g.Id == dto.GuiaId);

            if (!guiaExiste)
            {
                return NotFound(
                    "El guía especificado no existe."
                );
            }

            var transporteExiste = await _context.Transportes
                .AnyAsync(t => t.Id == dto.TransporteId);

            if (!transporteExiste)
            {
                return NotFound(
                    "El transporte especificado no existe."
                );
            }

            tour.Nombre = dto.Nombre;
            tour.PaisId = dto.PaisId;
            tour.DestinoId = dto.DestinoId;
            tour.CategoriaId = dto.CategoriaId;
            tour.GuiaId = dto.GuiaId;
            tour.TransporteId = dto.TransporteId;
            tour.Fecha = dto.Fecha;
            tour.Hora = dto.Hora;
            tour.Precio = dto.Precio;

            PrepararDatosCalculados(tour, destino);

            await _context.SaveChangesAsync();

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

        private void PrepararDatosCalculados(
            Tour tour,
            Destino destino)
        {
            tour.Itbis = tour.Precio * 0.18m;

            tour.DuracionDias = destino.DuracionDias;
            tour.DuracionHoras = destino.DuracionHoras;

            var fechaInicio = tour.Fecha.Date.Add(tour.Hora);

            tour.FechaFin = fechaInicio
                .AddDays(tour.DuracionDias)
                .AddHours(tour.DuracionHoras);

            tour.Estado = CalcularEstado(
                tour.Fecha,
                tour.Hora,
                tour.FechaFin
            );
        }

        private static string CalcularEstado(
            DateTime fecha,
            TimeSpan hora,
            DateTime fechaFin)
        {
            var fechaInicio = fecha.Date.Add(hora);
            var ahora = DateTime.Now;

            if (ahora < fechaInicio)
            {
                return "Disponible";
            }

            if (ahora <= fechaFin)
            {
                return "En curso";
            }

            return "Finalizado";
        }


        private async Task<TourRespuestaDto?>
            ObtenerTourRespuesta(int id)
        {
            var tour = await _context.Tours
                .Where(t => t.Id == id)
                .Select(t => new TourRespuestaDto
                {
                    Id = t.Id,
                    Nombre = t.Nombre,

                    Pais = t.Pais != null
                        ? t.Pais.Nombre
                        : string.Empty,

                    Destino = t.Destino != null
                        ? t.Destino.Nombre
                        : string.Empty,

                    Categoria = t.Categoria != null
                        ? t.Categoria.Nombre
                        : string.Empty,

                    Guia = t.Guia != null
                        ? t.Guia.Nombre
                        : string.Empty,

                    Transporte = t.Transporte != null
                        ? t.Transporte.Tipo
                        : string.Empty,

                    Fecha = t.Fecha,
                    Hora = t.Hora,
                    Precio = t.Precio,
                    Itbis = t.Itbis,
                    DuracionDias = t.DuracionDias,
                    DuracionHoras = t.DuracionHoras,
                    FechaFin = t.FechaFin,
                    Estado = t.Estado
                })
                .FirstOrDefaultAsync();

            if (tour != null)
            {
                tour.Estado = CalcularEstado(
                    tour.Fecha,
                    tour.Hora,
                    tour.FechaFin
                );
            }

            return tour;
        }
    }
}