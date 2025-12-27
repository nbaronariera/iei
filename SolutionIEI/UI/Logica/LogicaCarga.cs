using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UI.Entidades;
using UI.Parsers;
using UI.Parsers.ParsedObjects;
using UI.Wrappers;

namespace UI.Logica
{
    public class LogicaCarga
    {
        private readonly Persistencia.Persistencia _persistencia;

        public LogicaCarga()
        {
            _persistencia = new Persistencia.Persistencia();
        }

        public async Task<(List<ResultObject>, int, string, string)> ObtenerGal()
        {
            var extractor = new GALExtractor();
            return await extractor.LoadData();
        }

        public async Task<(List<ResultObject>, int, string, string)> ObtenerCat()
        {
            var extractor = new CATExtractor();
            return await extractor.LoadData();
        }

        public async Task<(List<ResultObject>, int, string, string)> ObtenerCV()
        {
            var extractor = new CVExtractor();
            return await extractor.LoadData();
        }

        public bool Clean()
        {
            try
            {
                using var contexto = new AppDbContext();
                Debug.WriteLine("[LOGICA] Eliminando estaciones...");
                contexto.Estaciones.RemoveRange(contexto.Estaciones);

                Debug.WriteLine("[LOGICA] Eliminando localidades...");
                contexto.Localidades.RemoveRange(contexto.Localidades);

                Debug.WriteLine("[LOGICA] Eliminando provincias...");
                contexto.Provincias.RemoveRange(contexto.Provincias);

                contexto.SaveChanges();
                Debug.WriteLine("[LOGICA] Base de datos limpiada correctamente.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGICA] ERROR limpiando DB: {ex.Message}");
                return false;
            }
        }
    }
}
