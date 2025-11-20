using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using UI.Entidades;
using UI.Helpers;
using UI.Parsers;
using UI.UI_Gestor;
//using UI.Wrappers;


namespace UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Debug.WriteLine("=== INICIANDO TEST DE SELENIUM ===");

            try
            {
                // 1. Instanciar tu clase
                // Si aplicaste la mejora de instanciar el driver en el constructor, 
                // esto abrirá el navegador ahora.
                var seleniumHelper = new CoordenadasSelenium();

                // 2. Datos de prueba (una dirección real de tu JSON)
                string direccion = "Avda. de Valencia, 168";
                string municipio = "Castelló de la Plana";

                Debug.WriteLine($"---> Buscando coordenadas para: {direccion}, {municipio}");

                // 3. Llamar a la función
                var coordenadas = seleniumHelper.ObtenerCoordenadas(direccion, municipio);

                // 4. Imprimir el resultado en la consola
                Debug.WriteLine("------------------------------------------------");
                Debug.WriteLine($"[RESULTADO] Latitud:  {coordenadas.Lat}");
                Debug.WriteLine($"[RESULTADO] Longitud: {coordenadas.Lng}");
                Debug.WriteLine("------------------------------------------------");

                // Comprobación visual rápida
                if (coordenadas.Lat != 0 && coordenadas.Lng != 0)
                {
                    Debug.WriteLine("¡ÉXITO! Se han recuperado coordenadas válidas.");
                }
                else
                {
                    Debug.WriteLine("FALLO: Las coordenadas son 0,0 (posible error en la búsqueda).");
                }

                // 5. Limpieza (Si implementaste Dispose/Quit en tu clase)
                // Si tu clase implementa IDisposable (como te sugerí antes):
                // ((IDisposable)seleniumHelper).Dispose(); 
                // Si usas la versión original que subiste, el driver se cierra solo dentro del método.
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR FATAL] {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }

            Debug.WriteLine("=== FIN DEL TEST ===");

            // Mantener la consola abierta si lo ejecutas en modo Debug
            // Console.ReadLine(); 
        }
    }
}

