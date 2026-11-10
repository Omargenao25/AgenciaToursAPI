using AgenciaToursAPI.Security;
using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AgenciaToursAPI.Controllers
{
    [Authorize(Roles = Roles.Personal)]
    [Route("api/[controller]")]
    [ApiController]
    public class ToursController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ToursController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TourRespuestaDto>>> GetTours()
        {
            var tours = await _context.Tours
                .AsNoTracking()
                .Select(t => new TourRespuestaDto
                {
                    Id = t.Id,
                    Nombre = t.Nombre,
                    ImagenUrl = t.ImagenUrl,
                    ImagenDestinoUrl = t.Destino != null ? t.Destino.ImagenUrl : null,
                    PaisId = t.PaisId,
                    DestinoId = t.DestinoId,
                    CategoriaId = t.CategoriaId,
                    GuiaId = t.GuiaId,
                    TransporteId = t.TransporteId,

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
                    Estado = t.Estado,
                    CapacidadMaxima = t.CapacidadMaxima,
                    CerradoOperativo = _context.CierresOperativosTour.Any(x => x.TourId == t.Id),
                    CuposReservados = t.Reservas.Where(r => r.Estado != EstadosReserva.Cancelada).Sum(r => r.CantidadPersonas)
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

    
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<TourRespuestaDto>> GetTour(int id)
        {
            var tour = await _context.Tours
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new TourRespuestaDto
                {
                    Id = t.Id,
                    Nombre = t.Nombre,
                    ImagenUrl = t.ImagenUrl,
                    ImagenDestinoUrl = t.Destino != null ? t.Destino.ImagenUrl : null,
                    PaisId = t.PaisId,
                    DestinoId = t.DestinoId,
                    CategoriaId = t.CategoriaId,
                    GuiaId = t.GuiaId,
                    TransporteId = t.TransporteId,

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
                    Estado = t.Estado,
                    CapacidadMaxima = t.CapacidadMaxima,
                    CerradoOperativo = _context.CierresOperativosTour.Any(x => x.TourId == t.Id),
                    CuposReservados = t.Reservas.Where(r => r.Estado != EstadosReserva.Cancelada).Sum(r => r.CantidadPersonas)
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


        public sealed class ActualizarAdelantoDto
        {
            [System.ComponentModel.DataAnnotations.Range(0, 100)]
            public int PorcentajeAdelanto { get; set; }

            [System.ComponentModel.DataAnnotations.Range(1, 168)]
            public int HorasLimiteAdelanto { get; set; } = 24;
        }

        [AllowAnonymous]
        [HttpGet("{id:int}/condiciones")]
        public async Task<IActionResult> CondicionesPublicas(int id)
        {
            var condiciones = await _context.Tours.AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new { t.PorcentajeAdelanto, t.HorasLimiteAdelanto })
                .SingleOrDefaultAsync();
            return condiciones is null ? NotFound() : Ok(condiciones);
        }

        [HttpGet("{id:int}/adelanto")]
        public async Task<IActionResult> ObtenerAdelanto(int id)
        {
            var tour = await _context.Tours.AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new { t.Id, t.Nombre, t.PorcentajeAdelanto, t.HorasLimiteAdelanto,
                    TieneReservas = t.Reservas.Any() })
                .SingleOrDefaultAsync();
            return tour is null ? NotFound() : Ok(tour);
        }

        [HttpPut("{id:int}/adelanto")]
        public async Task<IActionResult> ConfigurarAdelanto(int id, ActualizarAdelantoDto dto)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var tour = await _context.Tours
                .FromSqlInterpolated($"SELECT * FROM dbo.Tours WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
                .SingleOrDefaultAsync();
            if (tour is null) return NotFound();

            if (DateTime.Now >= tour.Fecha.Date.Add(tour.Hora))
                return Conflict("El tour ya comenzó.");

            // No cambiar las condiciones anunciadas a clientes que ya reservaron.
            if (await _context.Reservas.AnyAsync(r => r.TourId == id))
                return Conflict("Este tour ya tiene reservas. No se puede cambiar el porcentaje de adelanto.");

            tour.PorcentajeAdelanto = dto.PorcentajeAdelanto;
            tour.HorasLimiteAdelanto = dto.HorasLimiteAdelanto;
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<TourRespuestaDto>> PostTour(
            CrearTourDto dto)
        {
            var fechaInicio = dto.Fecha.Date.Add(dto.Hora);

            if (fechaInicio <= DateTime.Now)
            {
                return BadRequest(
                    "No se puede crear un tour con una fecha y hora pasadas."
                );
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

            var tour = new Tour
            {
                Nombre = dto.Nombre.Trim(),
                ImagenUrl = string.IsNullOrWhiteSpace(dto.ImagenUrl) ? null : dto.ImagenUrl.Trim(),
                PaisId = dto.PaisId,
                DestinoId = dto.DestinoId,
                CategoriaId = dto.CategoriaId,
                GuiaId = dto.GuiaId,
                TransporteId = dto.TransporteId,
                Fecha = dto.Fecha,
                Hora = dto.Hora,
                Precio = dto.Precio,
                CapacidadMaxima = dto.CapacidadMaxima
            };

            PrepararDatosCalculados(tour, destino);

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            var respuesta = await ObtenerTourRespuesta(tour.Id);

            if (respuesta == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
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
            int id,
            ActualizarTourDto dto)
        {
            await using var transaccion = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var tour = await _context.Tours
                .FromSqlInterpolated($"SELECT * FROM dbo.Tours WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
                .SingleOrDefaultAsync();

            if (tour == null)
            {
                return NotFound("El tour no existe.");
            }

            if (await _context.CierresOperativosTour.AnyAsync(x => x.TourId == id))
                return Conflict("No se puede modificar un tour con cierre operativo registrado.");

            var fechaInicioSolicitada = dto.Fecha.Date.Add(dto.Hora);

            if (fechaInicioSolicitada <= DateTime.Now)
            {
                return BadRequest(
                    "La fecha y hora del tour deben ser futuras."
                );
            }

            var reservados = await _context.Reservas
                .Where(r => r.TourId == id && r.Estado != EstadosReserva.Cancelada)
                .SumAsync(r => (int?)r.CantidadPersonas) ?? 0;

            if (dto.CapacidadMaxima < reservados)
                return Conflict($"La capacidad no puede ser menor que los {reservados} cupos ya reservados.");

            var tieneReservas = reservados > 0;

            if (tieneReservas)
            {
                var cambiaDatosLogisticos =
                    tour.PaisId != dto.PaisId ||
                    tour.DestinoId != dto.DestinoId ||
                    tour.GuiaId != dto.GuiaId ||
                    tour.TransporteId != dto.TransporteId ||
                    tour.Fecha != dto.Fecha ||
                    tour.Hora != dto.Hora;

                if (cambiaDatosLogisticos)
                {
                    return BadRequest(
                        "No se pueden modificar el país, destino, guía, transporte, fecha ni hora de un tour que tiene reservas asociadas."
                    );
                }
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

            tour.Nombre = dto.Nombre.Trim();
            tour.ImagenUrl = string.IsNullOrWhiteSpace(dto.ImagenUrl) ? null : dto.ImagenUrl.Trim();
            tour.PaisId = dto.PaisId;
            tour.DestinoId = dto.DestinoId;
            tour.CategoriaId = dto.CategoriaId;
            tour.GuiaId = dto.GuiaId;
            tour.TransporteId = dto.TransporteId;
            tour.Fecha = dto.Fecha;
            tour.Hora = dto.Hora;
            tour.Precio = dto.Precio;
            tour.CapacidadMaxima = dto.CapacidadMaxima;

            PrepararDatosCalculados(tour, destino);

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

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

            if (await _context.CierresOperativosTour.AnyAsync(x => x.TourId == id))
                return Conflict("No se puede modificar un tour con cierre operativo registrado.");

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

        private static void PrepararDatosCalculados(
            Tour tour,
            Destino destino)
        {
            tour.Itbis = Math.Round(
                tour.Precio * 0.18m,
                2
            );

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

        private async Task<TourRespuestaDto?> ObtenerTourRespuesta(int id)
        {
            var tour = await _context.Tours
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new TourRespuestaDto
                {
                    Id = t.Id,
                    Nombre = t.Nombre,
                    ImagenUrl = t.ImagenUrl,
                    ImagenDestinoUrl = t.Destino != null ? t.Destino.ImagenUrl : null,
                    PaisId = t.PaisId,
                    DestinoId = t.DestinoId,
                    CategoriaId = t.CategoriaId,
                    GuiaId = t.GuiaId,
                    TransporteId = t.TransporteId,

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
                    Estado = t.Estado,
                    CapacidadMaxima = t.CapacidadMaxima,
                    CerradoOperativo = _context.CierresOperativosTour.Any(x => x.TourId == t.Id),
                    CuposReservados = t.Reservas.Where(r => r.Estado != EstadosReserva.Cancelada).Sum(r => r.CantidadPersonas)
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