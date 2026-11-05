using AgenciaToursAPI.Security;
using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AgenciaToursAPI.Controllers
{
    [Authorize(Roles = Roles.Personal)]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

      
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes()
        {
            var clientes = await _context.Clientes
                .AsNoTracking()
                .ToListAsync();

            return Ok(clientes);
        }

        
        [HttpGet("{id}")]
        public async Task<ActionResult<Cliente>> GetCliente(int id)
        {
            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                return NotFound("El cliente no existe.");
            }

            return Ok(cliente);
        }

       
        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            NormalizarCliente(cliente);

            var cedulaExiste = await _context.Clientes
                .AnyAsync(c => c.Cedula == cliente.Cedula);

            if (cedulaExiste)
            {
                return BadRequest("Ya existe un cliente con esa cédula.");
            }

            var correoExiste = await _context.Clientes
                .AnyAsync(c => c.Correo == cliente.Correo);

            if (correoExiste)
            {
                return BadRequest("Ya existe un cliente con ese correo." );
            }

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCliente),
                new { id = cliente.Id },
                cliente
            );
        }

       
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCliente(
            int id,
            Cliente cliente)
        {
            if (id != cliente.Id)
            {
                return BadRequest( "El Id de la URL no coincide con el Id del cliente.");
            }

            var clienteActual = await _context.Clientes
                .FindAsync(id);

            if (clienteActual == null)
            {
                return NotFound("El cliente no existe.");
            }

            NormalizarCliente(cliente);

            var cedulaExiste = await _context.Clientes
                .AnyAsync(c =>
                    c.Cedula == cliente.Cedula &&
                    c.Id != id
                );

            if (cedulaExiste)
            {
                return BadRequest("Ya existe otro cliente con esa cédula.");
            }

            var correoExiste = await _context.Clientes
                .AnyAsync(c =>
                    c.Correo == cliente.Correo &&
                    c.Id != id
                );

            if (correoExiste)
            {
                return BadRequest("Ya existe otro cliente con ese correo.");
            }

            clienteActual.Nombre = cliente.Nombre;
            clienteActual.Apellido = cliente.Apellido;
            clienteActual.Cedula = cliente.Cedula;
            clienteActual.Telefono = cliente.Telefono;
            clienteActual.Correo = cliente.Correo;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var cliente = await _context.Clientes
                .FindAsync(id);

            if (cliente == null)
            {
                return NotFound("El cliente no existe.");
            }

            var tieneReservas = await _context.Reservas
                .AnyAsync(r => r.ClienteId == id);

            if (tieneReservas)
            {
                return BadRequest( "No se puede eliminar el cliente porque tiene reservas asociadas.");
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static void NormalizarCliente(Cliente cliente)
        {
            cliente.Nombre = cliente.Nombre.Trim();
            cliente.Apellido = cliente.Apellido.Trim();
            cliente.Cedula = cliente.Cedula.Trim();
            cliente.Telefono = cliente.Telefono.Trim();
            cliente.Correo = cliente.Correo.Trim().ToLower();
        }
    }
}