using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using UI.Parsers;

namespace UI.Wrappers
{
    public static class XMLaJSONConversor
    {
        public static string Ejecutar()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string csvPath = Path.Combine(baseDirectory, "Fuentes", "ITV-CATEntrega.xml");

            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"No se encontró el XML: {csvPath}");

            // 1️ Leer CSV
            var csvParser = new XMLParser();
            csvParser.Load(csvPath);
            var listaObjetos = csvParser.ParseList(); // lista de objetos 'gal'

            // 2 Convertir a JSON (acentos + formateado)
            var opcionesJson = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string jsonContent = JsonSerializer.Serialize(listaObjetos, opcionesJson);

            // 3 Guardar con UTF-8 sin BOM
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            string outputDir = Path.Combine(baseDirectory, "ArchivosFuenteConvertidos");
            Directory.CreateDirectory(outputDir);

            string jsonPath = Path.Combine(outputDir, "ITV-CAT.json");
            File.WriteAllText(jsonPath, jsonContent, utf8NoBom);

            Debug.WriteLine($"[OK] JSON generado de CAT con acentos y formato:\n    {jsonPath}");

            // 4 Abrir carpeta automáticamente
#if DEBUG
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = outputDir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch { }
#endif

            return jsonPath;
        }
    }
}
