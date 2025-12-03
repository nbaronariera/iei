using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using UI.Entidades;
using UI.Parsers;
using UI.UI_Gestor;
using UI.Wrappers;


namespace UI
{
    internal static class Program
    {
        private static IHost? _webHost;

        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        { 
            Task.Run(() => startServer());

            FormularioCarga mainForm = new FormularioCarga();
            //FormularioBusqueda mainForm = new FormularioBusqueda();
            mainForm.ShowDialog();

            stopServer().GetAwaiter().GetResult();

            /*
            
                Debug.WriteLine("=== INICIANDO CONVERSIÓN ===");

                // Borrar y recrear la base de datos al inicio
                using (var db = new AppDbContext())
                {
                    
                   // Debug.WriteLine("[INFO] Eliminando base de datos existente...");
                  //  db.Database.EnsureDeleted();

                   // Debug.WriteLine("[INFO] Creando base de datos vacía...");
                   // db.Database.EnsureCreated();
                    
                    
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

                var resultadosCat = catParser.FromParsedToUsefull(catParser.ParseList());
                var resultadosGal = galParser.FromParsedToUsefull(galParser.ParseList());
                var resultadosCv = cvParser.FromParsedToUsefull(cvParser.ParseList());

                var resultados = (
                    resultadosCat.Item1.Concat(resultadosGal.Item1).Concat(resultadosCv.Item1).ToList(),
                    resultadosCat.Item2 + resultadosCv.Item2 + resultadosGal.Item2,
                    resultadosCat.Item3 + resultadosCv.Item3 + resultadosGal.Item3
                );

                Debug.WriteLine($"[OK] {resultados.Item1.Count} estaciones parseadas.");
                Debug.WriteLine($"[OK] {resultados.Item2} estaciones parseadas correctamente.");
                Debug.WriteLine($"[OK] {resultados.Item3} estaciones omitidas.");

                Debug.WriteLine("=== ESTACIONES LISTAS PARA INSERTAR EN BASE DE DATOS ===");

                Debug.WriteLine("=== FIN ===");
            
            
            */
        }

        private static async void startServer()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddControllers();

            builder.WebHost.UseUrls("http://localhost:8080");
            var app = builder.Build();
            app.MapControllers();
            _webHost = app;
            await app.StartAsync();
        }

        private static async Task stopServer()
        {
            if (_webHost != null)
            {
                await _webHost.StopAsync();
                _webHost.Dispose();
            }
        }
    }
}

