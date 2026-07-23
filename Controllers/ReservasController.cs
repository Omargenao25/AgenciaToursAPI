using AgenciaToursAPI.Data;
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



        [HttpGet]
        public async Task<ActionResult<IEnumerable<Reserva>>> GetReservas()
        {
            return await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Tour)
                .Include(r => r.MetodoPago)
                .ToListAsync();
        }



        [HttpGet("{id}")]
        public async Task<ActionResult<Reserva>> GetReserva(int id)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Tour)
                .Include(r => r.MetodoPago)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null)
            {
                return NotFound("La reserva no existe.");
            }

            return reserva;
        }



        [HttpPost]
        public async Task<ActionResult<Reserva>> PostReserva(Reserva reserva)
        {
            var clienteExiste = await _context.Clientes
                .AnyAsync(c => c.Id == reserva.ClienteId);

            if (!clienteExiste)
            {
                return NotFound("El cliente especificado no existe.");
            }

            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.Id == reserva.TourId);

            if (tour == null)
            {
                return NotFound("El tour especificado no existe.");
            }

            var metodoPagoExiste = await _context.MetodosPago
                .AnyAsync(m => m.Id == reserva.MetodoPagoId);

            if (!metodoPagoExiste)
            {
                return NotFound("El método de pago especificado no existe.");
            }

            if (tour.Estado != "Disponible")
            {
                return BadRequest("El tour seleccionado no está disponible.");
            }

            reserva.FechaReserva = DateTime.Now;

            reserva.Total = tour.Precio * reserva.CantidadPersonas;

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetReserva),
                new { id = reserva.Id },
                reserva
            );
        }
    }
}