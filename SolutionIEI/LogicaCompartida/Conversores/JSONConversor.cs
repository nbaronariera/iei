using System.IO;
using UI.Helpers;
using UI.Parsers.ParsedObjects;

namespace UI.Wrappers
{
    public static class JSONConversor
    {
        public static string Ejecutar()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string jsonPath = Path.Combine(baseDirectory, "Fuentes", "estacionesEntrega2.json");

            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"No se encontró el JSON: {jsonPath}");


            return File.ReadAllText(jsonPath);
        }
    }
}
