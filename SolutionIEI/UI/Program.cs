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
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

            /*
            JSONParser Jsonparser = new JSONParser();
            CSVParser Csvparser = new CSVParser();
            GALParser Galparser = new GALParser();
            Csvparser.Load("W:\\iei\\SolutionIEI\\UI\\Fuentes\\Estacions_ITV.csv");

            var list = Csvparser.ParseList();
            var json = Jsonparser.toJSON(list);

            File.WriteAllText("W:\\iei\\SolutionIEI\\UI\\obj\\test.json", json, System.Text.Encoding.UTF8);
            Galparser.Load("W:\\iei\\SolutionIEI\\UI\\obj\\test.json");
            

            var lists = Galparser.ParseList();

            foreach (var e in lists){
                System.Console.WriteLine(e.ToString());
            }
            */

            try
            {
                Debug.WriteLine("=== INICIANDO CONVERSIÓN CSV → JSON ===");

                // Borrar y recrear la base de datos al inicio
                using (var db = new AppDbContext())
                {
                    Debug.WriteLine("[INFO] Eliminando base de datos existente...");
                    db.Database.EnsureDeleted();

                    Debug.WriteLine("[INFO] Creando base de datos vacía...");
                    db.Database.EnsureCreated();
                }

                // 1) Generar JSON desde CSV
                string archivoJSON = Wrapper_CSV_A_JSON.Ejecutar();
                //string _ = JSONConversor.Ejecutar();
                string __ = XMLaJSONConversor.Ejecutar();
                string archivoJSON = ""; //CSVaJSONConversor.Ejecutar();

                // 2) Cargar JSON usando GALParser
                var galParser = new GALParser();
                galParser.Load(archivoJSON);

                // 3) Convertir GALData → ResultObject (Provincia + Localidad + Estacion)
                var resultados = galParser.FromParsedToUsefull(galParser.ParseList());

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

