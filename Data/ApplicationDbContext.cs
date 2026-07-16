using Microsoft.EntityFrameworkCore;
using AgenciaToursAPI.Models;

namespace AgenciaToursAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Pais> Paises { get; set; }

        public DbSet<Destino> Destinos { get; set; }

        public DbSet<Categoria> Categorias { get; set; }

        public DbSet<Guia> Guias { get; set; }

        public DbSet<Transporte> Transportes { get; set; }

        public DbSet<Tour> Tours { get; set; }

        public DbSet<Cliente> Clientes { get; set; }

        public DbSet<MetodoPago> MetodosPago { get; set; }

        public DbSet<Reserva> Reservas { get; set; }

        public DbSet<Usuario> Usuarios { get; set; }
    }
}