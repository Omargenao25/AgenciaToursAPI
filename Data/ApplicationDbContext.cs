using Microsoft.EntityFrameworkCore;
using AgenciaToursAPI.Models;
using AgenciaToursAPI.Security;

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
        public DbSet<PasajeroReserva> PasajerosReserva { get; set; }
        public DbSet<CierreOperativoTour> CierresOperativosTour { get; set; }

        public DbSet<PagoReserva> PagosReserva { get; set; }
        public DbSet<ReembolsoReserva> ReembolsosReserva { get; set; }

        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<RecuperacionContrasena> RecuperacionesContrasena { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unicidad a nivel SQL para evitar registros duplicados por solicitudes concurrentes.
            modelBuilder.Entity<Pais>()
                .HasIndex(p => p.Nombre)
                .IsUnique();

            modelBuilder.Entity<Destino>()
                .HasIndex(d => new { d.PaisId, d.Nombre })
                .IsUnique();

            modelBuilder.Entity<Destino>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Destinos_DuracionPositiva",
                    "[DuracionDias] BETWEEN 0 AND 365 AND [DuracionHoras] BETWEEN 0 AND 23 AND ([DuracionDias] > 0 OR [DuracionHoras] > 0)"));

            modelBuilder.Entity<Destino>()
                .HasOne(d => d.Pais)
                .WithMany(p => p.Destinos)
                .HasForeignKey(d => d.PaisId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Tour>()
                .Property(t => t.CapacidadMaxima)
                .HasDefaultValue(100);

            modelBuilder.Entity<Tour>()
                .ToTable(t => t.HasCheckConstraint("CK_Tours_CapacidadMaxima", "[CapacidadMaxima] >= 1"));

            modelBuilder.Entity<Tour>()
                .Property(t => t.HorasLimiteAdelanto)
                .HasDefaultValue(24);

            modelBuilder.Entity<Tour>()
                .ToTable(t => t.HasCheckConstraint("CK_Tours_HorasLimiteAdelanto", "[HorasLimiteAdelanto] BETWEEN 1 AND 168"));

            modelBuilder.Entity<Tour>()
                .Property(t => t.PorcentajeAdelanto)
                .HasDefaultValue(30);

            modelBuilder.Entity<Tour>()
                .ToTable(t => t.HasCheckConstraint("CK_Tours_PorcentajeAdelanto", "[PorcentajeAdelanto] BETWEEN 0 AND 100"));

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
                .HasIndex(r => new { r.Estado, r.VencePendienteUtc });

            modelBuilder.Entity<Reserva>()
                .Property(r => r.Estado)
                .HasMaxLength(20)
                .HasDefaultValue(EstadosReserva.Confirmada);

            modelBuilder.Entity<Reserva>()
                .ToTable(t => t.HasCheckConstraint("CK_Reservas_Estado", "[Estado] IN (N'Pendiente', N'Confirmada', N'Cancelada')"));

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

            modelBuilder.Entity<CierreOperativoTour>()
                .HasOne(c => c.Tour)
                .WithMany()
                .HasForeignKey(c => c.TourId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CierreOperativoTour>()
                .HasIndex(c => c.TourId).IsUnique();

            modelBuilder.Entity<CierreOperativoTour>()
                .ToTable(t => t.HasCheckConstraint("CK_CierresOperativosTour_Contadores",
                    "[PlazasConfirmadas] >= 0 AND [PasajerosPresentes] >= 0 AND [PasajerosAusentes] >= 0 AND [PlazasConfirmadas] = [PasajerosPresentes] + [PasajerosAusentes]"));

            modelBuilder.Entity<PasajeroReserva>()
                .HasOne(p => p.Reserva)
                .WithMany()
                .HasForeignKey(p => p.ReservaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasajeroReserva>()
                .HasIndex(p => new { p.ReservaId, p.Orden })
                .IsUnique();

            modelBuilder.Entity<PasajeroReserva>()
                .ToTable(t => t.HasCheckConstraint("CK_PasajerosReserva_Orden", "[Orden] BETWEEN 1 AND 100"));

            modelBuilder.Entity<PagoReserva>()
                .HasOne(p => p.Reserva)
                .WithMany()
                .HasForeignKey(p => p.ReservaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PagoReserva>()
                .HasOne(p => p.MetodoPago)
                .WithMany()
                .HasForeignKey(p => p.MetodoPagoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PagoReserva>()
                .HasIndex(p => p.ClaveOperacion)
                .IsUnique();

            modelBuilder.Entity<PagoReserva>()
                .ToTable(t => t.HasCheckConstraint("CK_PagosReserva_Monto", "[Monto] > 0"));

            modelBuilder.Entity<ReembolsoReserva>()
                .HasOne(r => r.Reserva)
                .WithMany()
                .HasForeignKey(r => r.ReservaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReembolsoReserva>()
                .HasOne(r => r.MetodoPago)
                .WithMany()
                .HasForeignKey(r => r.MetodoPagoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReembolsoReserva>()
                .HasIndex(r => r.ClaveOperacion)
                .IsUnique();

            modelBuilder.Entity<ReembolsoReserva>()
                .ToTable(t => t.HasCheckConstraint("CK_ReembolsosReserva_Monto", "[Monto] > 0"));

            modelBuilder.Entity<RecuperacionContrasena>()
                .HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecuperacionContrasena>()
                .HasIndex(r => r.TokenHash)
                .IsUnique();

            modelBuilder.Entity<RecuperacionContrasena>()
                .HasIndex(r => new { r.UsuarioId, r.CreadoUtc });

            modelBuilder.Entity<Usuario>()
                .Property(u => u.Rol)
                .HasMaxLength(20)
                .HasDefaultValue(Roles.Cliente);

            modelBuilder.Entity<Usuario>()
                .ToTable(t => t.HasCheckConstraint("CK_Usuarios_Rol", "[Rol] IN (N'Administrador', N'Operador', N'Cliente')"));

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Cliente)
                .WithMany()
                .HasForeignKey(u => u.ClienteId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.ClienteId)
                .IsUnique()
                .HasFilter("[ClienteId] IS NOT NULL");

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Correo)
                .IsUnique();

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Cedula)
                .IsUnique()
                .HasFilter("[Cedula] IS NOT NULL");

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => new { c.TipoDocumento, c.PaisEmisorDocumento, c.NumeroDocumento })
                .IsUnique();

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Correo)
                .IsUnique();

            modelBuilder.Entity<Guia>()
                .HasIndex(g => g.Telefono)
                .IsUnique();
        }
    }
}