using Microsoft.EntityFrameworkCore;
using UI.Entidades;
using System.Collections.Generic;
using System.Linq;

namespace UI.Persistencia
{
    public class Persistencia
    {
        public List<Estacion> ObtenerEstaciones()
        {
            using var db = new AppDbContext();

            return db.Estaciones
                .Include(e => e.localidad)
                .ThenInclude(l => l.Provincia)
                .ToList();
        }

        public List<Provincia> ObtenerProvincias()
        {
            using var db = new AppDbContext();

            return db.Provincias
                .OrderBy(p => p.nombre)
                .ToList();
        }

        public List<Localidad> ObtenerLocalidades()
        {
            using var db = new AppDbContext();

            return db.Localidades
                .Include(l => l.Provincia)
                .OrderBy(l => l.Provincia.nombre)
                .ThenBy(l => l.nombre)
                .ToList();
        }
    }
}
