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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Destino>()
                .HasOne(d => d.Pais)
                .WithMany(p => p.Destinos)
                .HasForeignKey(d => d.PaisId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Tour>()
                .HasOne(t => t.Pais)
                .WithMany()
                .HasForeignKey(t => t.PaisId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Tour>()
                .HasOne(t => t.Destino)
                .WithMany(d => d.Tours)
                .HasForeignKey(t => t.DestinoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Tour>()
                .HasOne(t => t.Categoria)
                .WithMany(c => c.Tours)
                .HasForeignKey(t => t.CategoriaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Tour>()
                .HasOne(t => t.Guia)
                .WithMany(g => g.Tours)
                .HasForeignKey(t => t.GuiaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Tour>()
                .HasOne(t => t.Transporte)
                .WithMany(tr => tr.Tours)
                .HasForeignKey(t => t.TransporteId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Cliente)
                .WithMany(c => c.Reservas)
                .HasForeignKey(r => r.ClienteId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Tour)
                .WithMany(t => t.Reservas)
                .HasForeignKey(r => r.TourId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.MetodoPago)
                .WithMany(m => m.Reservas)
                .HasForeignKey(r => r.MetodoPagoId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}