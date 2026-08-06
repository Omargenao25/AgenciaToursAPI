using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReservasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReservasController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservaRespuestaDto>>>
            GetReservas()
        {
            var reservas = await _context.Reservas
                .AsNoTracking()
                .Select(r => new ReservaRespuestaDto
                {
                    Id = r.Id,

                    Cliente = r.Cliente != null
                        ? r.Cliente.Nombre + " " + r.Cliente.Apellido
                        : string.Empty,

                    Tour = r.Tour != null
                        ? r.Tour.Nombre
                        : string.Empty,

                    MetodoPago = r.MetodoPago != null
                        ? r.MetodoPago.Nombre
                        : string.Empty,

                    FechaReserva = r.FechaReserva,
                    CantidadPersonas = r.CantidadPersonas,

                    PrecioPorPersona = r.CantidadPersonas > 0
                        ? r.Total / r.CantidadPersonas
                        : 0,

                    Total = r.Total
                })
                .ToListAsync();

            return Ok(reservas);
        }

        // GET: api/Reservas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReservaRespuestaDto>>
            GetReserva(int id)
        {
            var reserva = await ObtenerReservaRespuesta(id);

            if (reserva == null)
            {
                return NotFound("La reserva no existe.");
            }

            return Ok(reserva);
        }

        // POST: api/Reservas
        [HttpPost]
        public async Task<ActionResult<ReservaRespuestaDto>>
            PostReserva(CrearReservaDto dto)
        {
            var clienteExiste = await _context.Clientes
                .AnyAsync(c => c.Id == dto.ClienteId);

            if (!clienteExiste)
            {
                return NotFound(
                    "El cliente especificado no existe."
                );
            }

            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.Id == dto.TourId);

            if (tour == null)
            {
                return NotFound(
                    "El tour especificado no existe."
                );
            }

            var fechaInicioTour = tour.Fecha.Date.Add(tour.Hora);

            if (DateTime.Now >= fechaInicioTour)
            {
                return BadRequest(
                    "No es posible realizar una reserva para un tour que ya inició."
                );
            }

            var metodoPagoExiste = await _context.MetodosPago
                .AnyAsync(m => m.Id == dto.MetodoPagoId);

            if (!metodoPagoExiste)
            {
                return NotFound(
                    "El método de pago especificado no existe."
                );
            }

            var reserva = new Reserva
            {
                ClienteId = dto.ClienteId,
                TourId = dto.TourId,
                MetodoPagoId = dto.MetodoPagoId,
                CantidadPersonas = dto.CantidadPersonas,
                FechaReserva = DateTime.Now,

                Total = Math.Round(
                    tour.Precio * dto.CantidadPersonas,
                    2
                )
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            var respuesta = await ObtenerReservaRespuesta(reserva.Id);

            if (respuesta == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "La reserva fue creada, pero no se pudo generar la respuesta."
                );
            }

            return CreatedAtAction(
                nameof(GetReserva),
                new { id = reserva.Id },
                respuesta
            );
        }

        
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReserva(
            int id,
            ActualizarReservaDto dto)
        {
            var reservaActual = await _context.Reservas
                .FindAsync(id);

            if (reservaActual == null)
            {
                return NotFound(
                    "La reserva no existe."
                );
            }

           
            var tourOriginal = await _context.Tours
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Id == reservaActual.TourId
                );

            if (tourOriginal == null)
            {
                return BadRequest(
                    "El tour original de la reserva ya no existe."
                );
            }

            var fechaInicioTourOriginal =
                tourOriginal.Fecha.Date.Add(tourOriginal.Hora);

            if (DateTime.Now >= fechaInicioTourOriginal)
            {
                return BadRequest(
                    "No se puede modificar una reserva cuyo tour ya inició."
                );
            }

            var clienteExiste = await _context.Clientes
                .AnyAsync(c => c.Id == dto.ClienteId);

            if (!clienteExiste)
            {
                return NotFound(
                    "El cliente especificado no existe."
                );
            }

            var tourNuevo = await _context.Tours
                .FirstOrDefaultAsync(t => t.Id == dto.TourId);

            if (tourNuevo == null)
            {
                return NotFound(
                    "El tour especificado no existe."
                );
            }

            var fechaInicioTourNuevo =
                tourNuevo.Fecha.Date.Add(tourNuevo.Hora);

            if (DateTime.Now >= fechaInicioTourNuevo)
            {
                return BadRequest(
                    "No es posible asignar la reserva a un tour que ya inició."
                );
            }

            var metodoPagoExiste = await _context.MetodosPago
                .AnyAsync(m => m.Id == dto.MetodoPagoId);

            if (!metodoPagoExiste)
            {
                return NotFound(
                    "El método de pago especificado no existe."
                );
            }

            reservaActual.ClienteId = dto.ClienteId;
            reservaActual.TourId = dto.TourId;
            reservaActual.MetodoPagoId = dto.MetodoPagoId;
            reservaActual.CantidadPersonas = dto.CantidadPersonas;

            reservaActual.Total = Math.Round(
                tourNuevo.Precio * dto.CantidadPersonas,
                2
            );

            /*
             * FechaReserva no cambia porque representa
             * el momento original en que se creó.
             */
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Reservas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReserva(int id)
        {
            var reserva = await _context.Reservas
                .FindAsync(id);

            if (reserva == null)
            {
                return NotFound(
                    "La reserva no existe."
                );
            }

            var tour = await _context.Tours
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == reserva.TourId);

            if (tour == null)
            {
                return BadRequest(
                    "El tour asociado a la reserva ya no existe."
                );
            }

            var fechaInicioTour = tour.Fecha.Date.Add(tour.Hora);

            if (DateTime.Now >= fechaInicioTour)
            {
                return BadRequest(
                    "No se puede eliminar una reserva de un tour que ya inició."
                );
            }

            _context.Reservas.Remove(reserva);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<ReservaRespuestaDto?>
            ObtenerReservaRespuesta(int id)
        {
            return await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new ReservaRespuestaDto
                {
                    Id = r.Id,

                    Cliente = r.Cliente != null
                        ? r.Cliente.Nombre + " " + r.Cliente.Apellido
                        : string.Empty,

                    Tour = r.Tour != null
                        ? r.Tour.Nombre
                        : string.Empty,

                    MetodoPago = r.MetodoPago != null
                        ? r.MetodoPago.Nombre
                        : string.Empty,

                    FechaReserva = r.FechaReserva,
                    CantidadPersonas = r.CantidadPersonas,

                    PrecioPorPersona = r.CantidadPersonas > 0
                        ? r.Total / r.CantidadPersonas
                        : 0,

                    Total = r.Total
                })
                .FirstOrDefaultAsync();
        }
    }
}