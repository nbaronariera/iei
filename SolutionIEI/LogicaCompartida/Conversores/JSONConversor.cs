using System.IO;
using UI.Helpers;
using UI.Parsers.ParsedObjects;

namespace UI.Wrappers
{
    /// <summary>
    /// Proporciona utilidades para la manipulación y lectura de archivos JSON.
    /// </summary>
    public static class JSONConversor
    {
        /// <summary>
        /// Lee el contenido del archivo 'estacionesEntrega2.json' ubicado en la carpeta Fuentes.
        /// </summary>
        /// <returns>Una cadena de texto con el contenido completo del archivo JSON.</returns>
        /// <exception cref="FileNotFoundException">Se lanza si el archivo no existe en la ruta especificada.</exception>
        public static string Ejecutar()
        {
            // Obtiene el directorio base donde se está ejecutando la aplicación
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Combina las rutas para obtener la ubicación exacta del archivo
            string jsonPath = Path.Combine(baseDirectory, "Fuentes", "estacionesEntrega2.json");

            // Validación de existencia antes de intentar la lectura
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"No se encontró el JSON: {jsonPath}");

            // Retorna el texto plano del archivo
            return File.ReadAllText(jsonPath);
        }
    }
}
