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
            Debug.WriteLine("[LOGICA CARGA] Creando GALExtractor");
            var galExtractor = new GALExtractor();
            return await galExtractor.LoadData(); // ← Ahora LoadData() devuelve tupla
        }

        public async Task<(List<ResultObject>, int, string, string)> ObtenerCat()
        {
            Debug.WriteLine("[LOGICA CARGA] Creando CATExtractor");
            var catExtractor = new CATExtractor();
            return await catExtractor.LoadData(); // ← Tupla
        }

        public async Task<(List<ResultObject>, int, string, string)> ObtenerCV()
        {
            Debug.WriteLine("[LOGICA] === INICIANDO CARGA CV ===");
            var cvExtractor = new CVExtractor();
            var resultado = await cvExtractor.LoadData();
            Debug.WriteLine($"[LOGICA] === CARGA CV FINALIZADA: {resultado.Item1.Count} estaciones ===");
            return resultado;
        }

        public bool Clean()
        {
            try
            {
                using var contexto = new AppDbContext();
                Console.WriteLine("[LOGICA] Eliminando estaciones...");
                contexto.Estaciones.RemoveRange(contexto.Estaciones);

                Console.WriteLine("[LOGICA] Eliminando localidades...");
                contexto.Localidades.RemoveRange(contexto.Localidades);

                Console.WriteLine("[LOGICA] Eliminando provincias...");
                contexto.Provincias.RemoveRange(contexto.Provincias);

                contexto.SaveChanges();
                Console.WriteLine("[LOGICA] Base de datos limpiada correctamente.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGICA] ERROR limpiando DB: {ex.Message}");
                return false;
            }
        }
    }
}
