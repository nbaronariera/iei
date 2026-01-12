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
    // Clase que actúa como fachada (Facade) para el proceso de ETL.
    // Centraliza la llamadas a los distintos extractores y gestiona la limpieza de la base de datos.
    public class LogicaCarga
    {
        private readonly Persistencia.Persistencia _persistencia;

        public LogicaCarga()
        {
            _persistencia = new Persistencia.Persistencia();
        }

        // Orquesta la carga de Galicia.
        // Utiliza GALExtractor para procesar los datos que provienen originalmente de CSV.
        public async Task<(List<ResultObject>, int, string, string)> ObtenerGal()
        {
            Debug.WriteLine("[LOGICA CARGA] Creando GALExtractor");
            var galExtractor = new GALExtractor();
            return await galExtractor.LoadData(); // ← Ahora LoadData() devuelve tupla
        }

        // Orquesta la carga de Cataluña.
        // Utiliza CATExtractor para procesar los datos específicos de Catalunya.
        public async Task<(List<ResultObject>, int, string, string)> ObtenerCat()
        {
            Debug.WriteLine("[LOGICA CARGA] Creando CATExtractor");
            var catExtractor = new CATExtractor();
            return await catExtractor.LoadData(); // ← Tupla
        }

        // Orquesta la carga de la Comunidad Valenciana.
        // 1. Instancia el extractor específico.
        // 2. Llama al método LoadData que se conecta al Wrapper y procesa el JSON.
        // 3. Devuelve las estadísticas.
        public async Task<(List<ResultObject>, int, string, string)> ObtenerCV()
        {
            Debug.WriteLine("[LOGICA] === INICIANDO CARGA CV ===");
            var cvExtractor = new CVExtractor();
            var resultado = await cvExtractor.LoadData();
            Debug.WriteLine($"[LOGICA] === CARGA CV FINALIZADA: {resultado.Item1.Count} estaciones ===");
            return resultado;
        }

        // Método para borrar y recrear la base de datos desde cero.
        // Útil para realizar una carga limpia y evitar duplicados o datos corruptos antiguos.
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
