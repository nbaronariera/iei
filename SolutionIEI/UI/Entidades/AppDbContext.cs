using Microsoft.EntityFrameworkCore;
using System.IO;

namespace UI.Entidades
{
    public class AppDbContext : DbContext
    {
        public DbSet<Provincia> Provincias { get; set; } = null!;
        public DbSet<Localidad> Localidades { get; set; } = null!;
        public DbSet<Estacion> Estaciones { get; set; } = null!;

        public AppDbContext()
        {
            // Asegura que la base de datos y tablas existen (útil en desarrollo).
            // Si prefieres manejar migraciones, elimina esta línea y usa migraciones EF Core.


            if (!File.Exists("Datos.db"))
            {
                Database.EnsureCreated();
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            
            
                optionsBuilder.UseSqlite("Data Source=Datos.db");
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Provincia
            modelBuilder.Entity<Provincia>(entity =>
            {
                entity.ToTable("Provincia");
                entity.HasKey(p => p.codigo);
                entity.Property(p => p.codigo).ValueGeneratedOnAdd();
                entity.Property(p => p.nombre).IsRequired();
                entity.HasMany(p => p.Localidades)
                      .WithOne(l => l.Provincia)
                      .HasForeignKey(l => l.codigoProvincia)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Localidad
            modelBuilder.Entity<Localidad>(entity =>
            {
                entity.ToTable("Localidad");
                entity.HasKey(l => l.codigo);
                entity.Property(l => l.codigo).ValueGeneratedOnAdd();
                entity.Property(l => l.nombre).IsRequired();
                entity.Property(l => l.codigoProvincia).IsRequired();
                entity.HasMany(l => l.Estaciones)
                      .WithOne(e => e.localidad)
                      .HasForeignKey(e => e.codigoLocalidad)
                      .OnDelete(DeleteBehavior.Cascade);
            });



            // Estacion
            modelBuilder.Entity<Estacion>(entity =>
            {
                entity.ToTable("Estacion");
                entity.HasKey(e => e.cod_estacion);
                entity.Property(e => e.cod_estacion).ValueGeneratedOnAdd();
                entity.Property(e => e.nombre).IsRequired();
                entity.Property(e => e.tipo).HasConversion<string>().IsRequired();
                entity.Property(e => e.direccion);
                entity.Property(e => e.codigoPostal);
                entity.Property(e => e.longitud);
                entity.Property(e => e.latitud);
                entity.Property(e => e.descripcion);
                entity.Property(e => e.horario);
                entity.Property(e => e.contacto);
                entity.Property(e => e.URL);
                entity.Property(e => e.codigoLocalidad).IsRequired();
            });
        }
    }
}
