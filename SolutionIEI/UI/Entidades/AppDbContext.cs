using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Entidades
{
    internal class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public System.Data.Entity.DbSet<Provincia> Provincias { get; set; } = null!;
        public System.Data.Entity.DbSet<Localidad> Localidades { get; set; } = null!;
        public System.Data.Entity.DbSet<Estacion> Estaciones { get; set; } = null!;

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
                entity.Property(p => p.nombre);
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
                entity.Property(l => l.nombre);
                entity.Property(l => l.codigoProvincia).IsRequired();
                entity.HasMany(l => l.Estaciones)
                      .WithOne(e => e.localidad)
                      .HasForeignKey(e => e.codigoLocalidad)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Estacion>(entity =>
            {
                entity.ToTable("Estacion");
                entity.HasKey(e => e.cod_estacion);
                entity.Property(e => e.cod_estacion).ValueGeneratedOnAdd();
                entity.Property(e => e.nombre);
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
