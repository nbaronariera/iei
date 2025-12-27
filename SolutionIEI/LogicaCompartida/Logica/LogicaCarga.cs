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

        public async Task<string> ObtenerGal()
        {
            var extractor = new GALExtractor();
            (List<ResultObject>, int, string, string) result = await extractor.LoadData(); 
            string res = $"Cargado Galicia.\n Añadidas: {result.Item1.Count}, Omitidas: {result.Item2}. Log:\nAñadidas:\n{result.Item3}\nOmitidas:\n{result.Item4}";
            return res; 
        }

        public async Task<string> ObtenerCat()
        {
            var extractor = new CATExtractor();
            (List<ResultObject>, int, string, string) result = await extractor.LoadData(); 
            Console.WriteLine(result.Item3);
            string res = $"Cargado Cataluña.\n Añadidas: {result.Item1.Count}, Omitidas: {result.Item2}. Log:\nAñadidas:\n{result.Item3}\nOmitidas:\n{result.Item4}";
            return res;
        }

        public async Task<string> ObtenerCV()
        {
            var extractor = new CVExtractor();
            (List<ResultObject>, int, string, string) result = await extractor.LoadData(); 
            string res = $"Cargado Comunidad Valenciana.\n Añadidas: {result.Item1.Count}, Omitidas: {result.Item2}. Log:\nAñadidas:\n{result.Item3}\nOmitidas:\n{result.Item4}";
            return res;
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
