using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using UI.Entidades;
using UI.Parsers;
using UI.UI_Gestor;
using UI.Wrappers;


namespace UI
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Debug.WriteLine("=== INICIANDO CONVERSIÓN ===");

                // Borrar y recrear la base de datos al inicio
                using (var db = new AppDbContext())
                {
                    Debug.WriteLine("[INFO] Eliminando base de datos existente...");
                    db.Database.EnsureDeleted();

                    Debug.WriteLine("[INFO] Creando base de datos vacía...");
                    db.Database.EnsureCreated();
                }

                string JsonCV = JSONConversor.Ejecutar();
                string JsonCAT = XMLaJSONConversor.Ejecutar();
                string JsonGAL = CSVaJSONConversor.Ejecutar();

                // 2) Cargar JSON usando GALParser
                
                var galParser = new GALExtractor();
                galParser.Load(JsonGAL);
                
                var catParser = new CATExtractor();
                catParser.Load(JsonCAT);

                var cvParser = new CVExtractor();
                cvParser.Load(JsonCV);

                var resultados = catParser.FromParsedToUsefull(catParser.ParseList())
                    .Concat(galParser.FromParsedToUsefull(galParser.ParseList()))
                    .Concat(cvParser.FromParsedToUsefull(cvParser.ParseList())).ToList();

                Debug.WriteLine($"[OK] {resultados.Count} estaciones parseadas correctamente.");

                Debug.WriteLine("=== ESTACIONES LISTAS PARA INSERTAR EN BASE DE DATOS ===");

                // 4) Mostrar cada ResultObject con ToString completo
                foreach (var r in resultados)
                {
                    Debug.WriteLine(r.ToString());
                }

                Debug.WriteLine("=== FIN ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] {ex.GetType().Name}: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }

        }
    }
}

