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
                    TourId = r.TourId,
                    FechaTour = r.Tour != null ? r.Tour.Fecha : null,
                    HoraTour = r.Tour != null ? r.Tour.Hora : null,

                    MetodoPago = r.MetodoPago != null
                        ? r.MetodoPago.Nombre
                        : string.Empty,

                    FechaReserva = r.FechaReserva,
                    CantidadPersonas = r.CantidadPersonas,

                    PrecioPorPersona = r.CantidadPersonas > 0
                    ? Math.Round(r.Total / r.CantidadPersonas, 2)
                    : 0,

                    Total = r.Total,
                    Estado = r.Estado,
                    FechaCambioEstado = r.FechaCambioEstado
                })
                .ToListAsync();

            return Ok(reservas);
        }

       
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

     
        [HttpPost]
        public async Task<ActionResult<ReservaRespuestaDto>>
            PostReserva(CrearReservaDto dto)
        {
            if (!ValidacionPasajeros.IntentarNormalizar(dto.CantidadPersonas, dto.Pasajeros, out var pasajeros, out var error))
                return BadRequest(error);
            await using var transaccion = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var clienteExiste = await _context.Clientes
                .AnyAsync(c => c.Id == dto.ClienteId);

            if (!clienteExiste)
            {
                return NotFound(
                    "El cliente especificado no existe."
                );
            }

            var tour = await _context.Tours
                .FromSqlInterpolated($"SELECT * FROM dbo.Tours WITH (UPDLOCK, HOLDLOCK) WHERE Id = {dto.TourId}")
                .SingleOrDefaultAsync();

            if (tour == null)
            {
                return NotFound(
                    "El tour especificado no existe."
                );
            }

            if (await _context.CierresOperativosTour.AnyAsync(c => c.TourId == tour.Id))
                return Conflict("La salida ya fue cerrada operativamente. No se permiten reservas nuevas.");

            var fechaInicioTour = tour.Fecha.Date.Add(tour.Hora);

            if (DateTime.Now >= fechaInicioTour)
            {
                return BadRequest(
                    "No es posible realizar una reserva para un tour que ya inició."
                );
            }

            var reservados = await _context.Reservas
                .Where(r => r.TourId == dto.TourId && r.Estado != EstadosReserva.Cancelada)
                .SumAsync(r => (int?)r.CantidadPersonas) ?? 0;

            if ((long)reservados + dto.CantidadPersonas > tour.CapacidadMaxima)
                return Conflict($"Cupos insuficientes. Disponibles: {Math.Max(0, tour.CapacidadMaxima - reservados)}.");

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
                VencePendienteUtc = DateTime.UtcNow.AddHours(tour.HorasLimiteAdelanto),
                Estado = EstadosReserva.Pendiente,

                Total = Math.Round(
                    tour.Precio * dto.CantidadPersonas,
                    2
                )
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();
            foreach (var pasajero in pasajeros) pasajero.ReservaId = reserva.Id;
            _context.PasajerosReserva.AddRange(pasajeros);
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

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
            await using var transaccion = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var reservaActual = await _context.Reservas
                .FromSqlInterpolated($"SELECT * FROM dbo.Reservas WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
                .SingleOrDefaultAsync();

            if (reservaActual == null)
            {
                return NotFound("La reserva no existe.");
            }

            if (reservaActual.Estado == EstadosReserva.Cancelada)
                return Conflict("Una reserva cancelada no puede modificarse.");

            if (await _context.PagosReserva.AnyAsync(p => p.ReservaId == id))
                return Conflict("La reserva tiene pagos registrados. No es posible editar su importe o titular sin conciliación.");

           
            // Bloquear ambos tours en orden fijo para evitar sobreventa y reducir interbloqueos.
            var idsTour = new[] { reservaActual.TourId, dto.TourId }.Distinct().OrderBy(x => x).ToArray();
            var toursBloqueados = new Dictionary<int, Tour>();
            foreach (var tourId in idsTour)
            {
                var actual = await _context.Tours
                    .FromSqlInterpolated($"SELECT * FROM dbo.Tours WITH (UPDLOCK, HOLDLOCK) WHERE Id = {tourId}")
                    .SingleOrDefaultAsync();
                if (actual != null) toursBloqueados[tourId] = actual;
            }
            if (await _context.CierresOperativosTour.AnyAsync(c => idsTour.Contains(c.TourId)))
                return Conflict("No se pueden modificar reservas asociadas a tours con cierre operativo.");
            toursBloqueados.TryGetValue(reservaActual.TourId, out var tourOriginal);

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

            toursBloqueados.TryGetValue(dto.TourId, out var tourNuevo);

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

            var reservadosDestino = await _context.Reservas
                .Where(r => r.TourId == dto.TourId && r.Id != id && r.Estado != EstadosReserva.Cancelada)
                .SumAsync(r => (int?)r.CantidadPersonas) ?? 0;
            if ((long)reservadosDestino + dto.CantidadPersonas > tourNuevo.CapacidadMaxima)
                return Conflict($"Cupos insuficientes. Disponibles: {Math.Max(0, tourNuevo.CapacidadMaxima - reservadosDestino)}.");

            var metodoPagoExiste = await _context.MetodosPago
                .AnyAsync(m => m.Id == dto.MetodoPagoId);

            if (!metodoPagoExiste)
            {
                return NotFound(
                    "El método de pago especificado no existe."
                );
            }

            var cantidadPasajerosRegistrados = await _context.PasajerosReserva
                .CountAsync(p => p.ReservaId == id);
            if (cantidadPasajerosRegistrados > 0 && cantidadPasajerosRegistrados != dto.CantidadPersonas)
                return Conflict("No puedes cambiar la cantidad de plazas sin actualizar los pasajeros registrados.");

            reservaActual.ClienteId = dto.ClienteId;
            reservaActual.TourId = dto.TourId;
            reservaActual.MetodoPagoId = dto.MetodoPagoId;
            reservaActual.CantidadPersonas = dto.CantidadPersonas;

            reservaActual.Total = Math.Round(
                tourNuevo.Precio * dto.CantidadPersonas,
                2
            );

           
            
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return NoContent();
        }

       
        [HttpPatch("{id}/confirmar")]
        public async Task<IActionResult> ConfirmarReserva(int id)
        {
            await using var transaccion = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var reserva = await _context.Reservas
                .FromSqlInterpolated($"SELECT * FROM dbo.Reservas WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
                .SingleOrDefaultAsync();
            if (reserva == null) return NotFound("La reserva no existe.");
            if (reserva.Estado == EstadosReserva.Cancelada)
                return Conflict("Una reserva cancelada no se puede confirmar.");
            if (reserva.Estado == EstadosReserva.Confirmada)
                return NoContent();

            var tour = await _context.Tours
                .FromSqlInterpolated($"SELECT * FROM dbo.Tours WITH (UPDLOCK, HOLDLOCK) WHERE Id = {reserva.TourId}")
                .SingleOrDefaultAsync();

            if (tour == null) return NotFound("El tour asociado no existe.");
            if (await _context.CierresOperativosTour.AnyAsync(c => c.TourId == reserva.TourId))
                return Conflict("El tour tiene un cierre operativo y sus reservas están bloqueadas.");
            if (DateTime.Now >= tour.Fecha.Date.Add(tour.Hora))
                return Conflict("No es posible confirmar una reserva de un tour que ya comenzó.");

            // Confirmación financiera: el importe mínimo se redondea siempre
            // hacia arriba al centavo; 30 % de RD$ 100.01 requiere RD$ 30.01.
            var minimo = Math.Ceiling(reserva.Total * tour.PorcentajeAdelanto) / 100m;
            var pagado = await _context.PagosReserva
                .Where(p => p.ReservaId == id)
                .SumAsync(p => (decimal?)p.Monto) ?? 0m;
            var devuelto = await _context.ReembolsosReserva
                .Where(x => x.ReservaId == id)
                .SumAsync(x => (decimal?)x.Monto) ?? 0m;
            var neto = pagado - devuelto;
            // Si venció sin que se registrara ningún pago, no permitir
            // confirmación manual mientras el proceso periódico libera los cupos.
            if (reserva.VencePendienteUtc is DateTime vencimiento &&
                vencimiento <= DateTime.UtcNow && pagado == 0m)
                return Conflict("El plazo de adelanto venció sin pagos. Esta solicitud será cancelada automáticamente.");

            if (neto < minimo)
                return Conflict($"Se requiere un adelanto neto del {tour.PorcentajeAdelanto}% (RD$ {minimo:N2}) para confirmar. Neto registrado: RD$ {neto:N2}; faltante: RD$ {minimo - neto:N2}.");

            reserva.Estado = EstadosReserva.Confirmada;
            reserva.FechaCambioEstado = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
            return NoContent();
        }

        [HttpPatch("{id}/cancelar")]
        public async Task<IActionResult> CancelarReserva(int id)
        {
            await using var transaccion = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var reserva = await _context.Reservas
                .FromSqlInterpolated($"SELECT * FROM dbo.Reservas WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
                .SingleOrDefaultAsync();
            if (reserva == null) return NotFound("La reserva no existe.");
            // Solo puede cancelarse después de registrar todas las devoluciones.
            // Los pagos originales permanecen inmutables para auditoría.
            var cobrado = await _context.PagosReserva.Where(p => p.ReservaId == id)
                .SumAsync(p => (decimal?)p.Monto) ?? 0m;
            var devuelto = await _context.ReembolsosReserva.Where(p => p.ReservaId == id)
                .SumAsync(p => (decimal?)p.Monto) ?? 0m;
            if (cobrado != devuelto)
                return Conflict($"No se puede cancelar: falta conciliar RD$ {cobrado - devuelto:N2} de pagos registrados.");

            var tour = await _context.Tours
                .FromSqlInterpolated($"SELECT * FROM dbo.Tours WITH (UPDLOCK, HOLDLOCK) WHERE Id = {reserva.TourId}")
                .SingleOrDefaultAsync();

            if (tour == null) return NotFound("El tour asociado no existe.");
            if (await _context.CierresOperativosTour.AnyAsync(c => c.TourId == reserva.TourId))
                return Conflict("El tour tiene un cierre operativo y sus reservas están bloqueadas.");
            if (reserva.Estado == EstadosReserva.Cancelada) return NoContent();
            if (DateTime.Now >= tour.Fecha.Date.Add(tour.Hora))
                return Conflict("No se puede cancelar una reserva después del inicio del tour.");

            reserva.Estado = EstadosReserva.Cancelada;
            reserva.MotivoCancelacion = cobrado > 0m ? "ReembolsoConciliado" : "CancelacionManual";
            reserva.FechaCambioEstado = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
            return NoContent();
        }

        // No eliminamos el historial de reservas: el DELETE realiza una cancelación lógica.
        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteReserva(int id) => CancelarReserva(id);

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
                    TourId = r.TourId,
                    FechaTour = r.Tour != null ? r.Tour.Fecha : null,
                    HoraTour = r.Tour != null ? r.Tour.Hora : null,

                    MetodoPago = r.MetodoPago != null
                        ? r.MetodoPago.Nombre
                        : string.Empty,

                    FechaReserva = r.FechaReserva,
                    CantidadPersonas = r.CantidadPersonas,


                    PrecioPorPersona = r.CantidadPersonas > 0
                    ? Math.Round(r.Total / r.CantidadPersonas, 2)
                    : 0,

                    Total = r.Total,
                    Estado = r.Estado,
                    FechaCambioEstado = r.FechaCambioEstado
                })
                .FirstOrDefaultAsync();
        }
    }
}