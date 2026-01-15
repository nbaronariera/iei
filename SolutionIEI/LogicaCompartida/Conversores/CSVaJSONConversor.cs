using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using UI.Parsers;
using UI.Parsers.ParsedObjects;

namespace UI.Wrappers
{
    /// <summary>
    /// Proporciona utilidades para la conversión de datos entre formatos CSV y JSON.
    /// </summary>
  
    public static class CSVaJSONConversor
    {
        /// <summary>
        /// Lee un archivo CSV de estaciones de ITV, lo procesa y genera un archivo JSON 
        /// manteniendo las cabeceras originales del dominio.
        /// </summary>
        /// <returns>Una cadena de texto con el contenido JSON generado.</returns>
        /// <exception cref="FileNotFoundException">Se lanza si el archivo CSV de origen no existe.</exception>
        public static string Ejecutar()
        {
            // 1. Configuración de rutas y validación de existencia del archivo
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string csvPath = Path.Combine(baseDirectory, "Fuentes", "Estacions_ITVEntrega2.csv");

            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"No se encontró el CSV: {csvPath}");

            // 2. Parseo del CSV a objetos de dominio (listaGAL)
            var csvParser = new CSVParser();
            csvParser.Load(csvPath);
            var listaGAL = csvParser.ParseList();

            // Log de control para depuración en consola
            Console.WriteLine($"[CSVaJSON] CSV parseado correctamente → {listaGAL.Count} registros GAL");
            if (listaGAL.Count > 0)
            {
                var g = listaGAL[0];
                Console.WriteLine($"[CSVaJSON] Primera estación:");
                Console.WriteLine($"  Nombre: {g.NombreEstacion}");
                Console.WriteLine($"  Municipio: {g.Municipio}");
                Console.WriteLine($"  Provincia: {g.Provincia}");
                Console.WriteLine($"  CP: {g.CodigoPostal}");
            }

            // 3. Transformación: Mapeo de objetos tipados a Diccionarios
            // Se utilizan diccionarios para forzar que las claves del JSON coincidan exactamente
            // con las cabeceras originales (incluyendo espacios y tildes).
            var listaDiccionarios = new List<Dictionary<string, string>>();
            foreach (var fila in listaGAL)
            {
                var dict = new Dictionary<string, string>
                {
                    ["NOME DA ESTACIÓN"] = fila.NombreEstacion ?? "",
                    ["ENDEREZO"] = fila.Direccion ?? "",
                    ["CONCELLO"] = fila.Municipio ?? "",
                    ["CÓDIGO POSTAL"] = fila.CodigoPostal ?? "",
                    ["PROVINCIA"] = fila.Provincia ?? "",
                    ["TELÉFONO"] = fila.Telefono ?? "",
                    ["HORARIO"] = fila.HorarioRaw ?? "",
                    ["SOLICITUDE DE CITA PREVIA"] = fila.UrlCita ?? "",
                    ["CORREO ELECTRÓNICO"] = fila.Correo ?? "",
                    ["COORDENADAS GMAPS"] = fila.Coordenadas ?? ""
                };
                listaDiccionarios.Add(dict);
            }

            // 4. Configuración del Serializador JSON
            // WriteIndented: Formatea el JSON con espacios para que sea legible.
            // UnsafeRelaxedJsonEscaping: Permite caracteres especiales (tildes, ñ) sin escapado Unicode.
            var opcionesJson = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string jsonContent = JsonSerializer.Serialize(listaDiccionarios, opcionesJson);

            // 5. Persistencia: Guardado del archivo resultante
            string outputDir = Path.Combine(baseDirectory, "ArchivosFuenteConvertidos");
            Directory.CreateDirectory(outputDir);

            string jsonPath = Path.Combine(outputDir, "Estacions_ITV.json");

            // Se usa UTF-8 sin BOM para máxima compatibilidad con sistemas web
            var utf8NoBom = new UTF8Encoding(false);
            File.WriteAllText(jsonPath, jsonContent, utf8NoBom);

            Console.WriteLine($"[CSVaJSON] JSON generado correctamente con cabeceras originales:");
            Console.WriteLine($"→ {jsonPath}");

            return jsonContent;
        }
    }
}