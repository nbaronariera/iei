using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using UI.Parsers;

namespace UI.Wrappers
{
    /// <summary>
    /// Proporciona utilidades para la conversión de datos entre formatos XML y JSON.
    /// </summary>
    public static class XMLaJSONConversor
    {
        /// <summary>
        /// Lee un archivo XML de estaciones ITV, lo procesa y genera un archivo JSON formateado.
        /// </summary>
        /// <returns>Una cadena de texto con el contenido JSON generado.</returns>
        /// <exception cref="FileNotFoundException">Se lanza si el archivo XML de origen no existe.</exception>
        
        public static string Ejecutar()
          {
            // Definición de rutas: Se busca el archivo en la carpeta 'Fuentes' dentro del directorio de ejecución
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string csvPath = Path.Combine(baseDirectory, "Fuentes", "ITV-CATEntrega2.xml");

            // Validación de existencia del recurso
            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"No se encontró el XML: {csvPath}");

            // 1. PROCESAMIENTO DEL XML
            // Se utiliza el parser personalizado para cargar y mapear el XML a una lista de objetos
            var csvParser = new XMLParser();
            csvParser.Load(csvPath);
            var listaObjetos = csvParser.ParseList(); // lista de objetos 'gal'

            // 2. CONFIGURACIÓN DE SERIALIZACIÓN JSON
            // Se configura para que sea legible (indented) y respete caracteres especiales (acentos)
            var opcionesJson = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string jsonContent = JsonSerializer.Serialize(listaObjetos, opcionesJson);

            // 3. PERSISTENCIA EN DISCO
            // Creamos el directorio de salida si no existe y guardamos con codificación UTF-8 pura
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            string outputDir = Path.Combine(baseDirectory, "ArchivosFuenteConvertidos");
            Directory.CreateDirectory(outputDir);

            string jsonPath = Path.Combine(outputDir, "ITV-CAT.json");
            File.WriteAllText(jsonPath, jsonContent, utf8NoBom);

            Console.WriteLine($"[OK] JSON generado de CAT con acentos y formato:\n    {jsonPath}");

            return jsonContent;
        }

        
    }
}