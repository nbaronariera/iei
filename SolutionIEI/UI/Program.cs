using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using UI.UI_Gestor;


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
            Console.WriteLine($" ARRANCANDO UI");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Bucle que mantiene la aplicación viva
            while (true)
            {
                using (FormularioBusqueda frmBusqueda = new FormularioBusqueda())
                {
                    // Si el usuario cierra búsqueda sin ir a carga, salir de la app
                    if (frmBusqueda.ShowDialog() != DialogResult.OK)
                    {
                        return; // Sale del bucle y termina la app
                    }
                }

                // Si llega aquí, es porque desde búsqueda se pidió abrir carga
                using (FormularioCarga frmCarga = new FormularioCarga())
                {
                    // Si el usuario cierra carga sin volver a búsqueda, salir
                    if (frmCarga.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                }

                // Si llega aquí, volver a abrir búsqueda (nueva instancia con datos frescos)
            }
        }
    }
}

