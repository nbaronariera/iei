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
            FormularioBusqueda mainForm = new FormularioBusqueda();
            mainForm.ShowDialog();

           stopServer().GetAwaiter().GetResult();
        }

        private static async void startServer()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddControllers();

            builder.WebHost.UseUrls("http://localhost:5001");
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

