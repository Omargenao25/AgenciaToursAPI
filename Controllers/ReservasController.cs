using AgenciaToursAPI.Data;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenciaToursAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReservasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Reservas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservaRespuestaDto>>>
            GetReservas()
        {
            var reservas = await _context.Reservas
                .Select(r => new ReservaRespuestaDto
                {
                    Id = r.Id,

                    Cliente = r.Cliente != null
                        ? r.Cliente.Nombre
                        : string.Empty,

                    Tour = r.Tour != null
                        ? r.Tour.Nombre
                        : string.Empty,

                    MetodoPago = r.MetodoPago != null
                        ? r.MetodoPago.Nombre
                        : string.Empty,

                    FechaReserva = r.FechaReserva,
                    CantidadPersonas = r.CantidadPersonas,

                    PrecioPorPersona = r.Tour != null
                        ? r.Tour.Precio
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

            /*
             * La fecha de reserva representa el momento en que
             * el cliente realiza la reservación.
             *
             * La fecha y hora del tour representan el momento
             * en que comienza el viaje.
             */
            var fechaInicioTour = tour.Fecha.Date.Add(tour.Hora);

            if (DateTime.Now >= fechaInicioTour)
            {
                return BadRequest(
                    "No es posible realizar una reserva para un tour que ya inició."
                );
            }

            var estadoActual = CalcularEstadoTour(tour);

            if (estadoActual != "Disponible")
            {
                return BadRequest(
                    "El tour seleccionado no está disponible."
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
                Total = tour.Precio * dto.CantidadPersonas
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            var respuesta = await ObtenerReservaRespuesta(reserva.Id);

            if (respuesta == null)
            {
                return StatusCode(
                    500,
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
                    "No es posible asignar la reserva a un tour que ya inició."
                );
            }

            var estadoActual = CalcularEstadoTour(tour);

            if (estadoActual != "Disponible")
            {
                return BadRequest("El tour seleccionado no está disponible.");
            }

            var metodoPagoExiste = await _context.MetodosPago
                .AnyAsync(m => m.Id == dto.MetodoPagoId);

            if (!metodoPagoExiste)
            {
                return NotFound( "El método de pago especificado no existe.");
            }

            reservaActual.ClienteId = dto.ClienteId;
            reservaActual.TourId = dto.TourId;
            reservaActual.MetodoPagoId = dto.MetodoPagoId;
            reservaActual.CantidadPersonas = dto.CantidadPersonas;

            reservaActual.Total =
                tour.Precio * dto.CantidadPersonas;

            await _context.SaveChangesAsync();

            return NoContent();
        }

       

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

            _context.Reservas.Remove(reserva);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static string CalcularEstadoTour(Tour tour)
        {
            var fechaInicio = tour.Fecha.Date.Add(tour.Hora);
            var ahora = DateTime.Now;

            if (ahora < fechaInicio)
            {
                return "Disponible";
            }

            if (ahora <= tour.FechaFin)
            {
                return "En curso";
            }

            return "Finalizado";
        }



        private async Task<ReservaRespuestaDto?>
            ObtenerReservaRespuesta(int id)
        {
            return await _context.Reservas
                .Where(r => r.Id == id)
                .Select(r => new ReservaRespuestaDto
                {
                    Id = r.Id,

                    Cliente = r.Cliente != null
                        ? r.Cliente.Nombre
                        : string.Empty,

                    Tour = r.Tour != null
                        ? r.Tour.Nombre
                        : string.Empty,

                    MetodoPago = r.MetodoPago != null
                        ? r.MetodoPago.Nombre
                        : string.Empty,

                    FechaReserva = r.FechaReserva,
                    CantidadPersonas = r.CantidadPersonas,

                    PrecioPorPersona = r.Tour != null
                        ? r.Tour.Precio
                        : 0,

                    Total = r.Total
                })
                .FirstOrDefaultAsync();
        }
    }
}