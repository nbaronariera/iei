using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using UI.Parsers;
using UI.Parsers.ParsedObjects;

namespace UI.Wrappers
{
    public static class CSVaJSONConversor
    {
        public static string Ejecutar()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string csvPath = Path.Combine(baseDirectory, "Fuentes", "Estacions_ITVEntrega.csv");
            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"No se encontró el CSV: {csvPath}");

            var csvParser = new CSVParser();
            csvParser.Load(csvPath);
            var listaGAL = csvParser.ParseList();

            Debug.WriteLine($"[CSVaJSON] CSV parseado correctamente → {listaGAL.Count} registros GAL");
            if (listaGAL.Count > 0)
            {
                var g = listaGAL[0];
                Debug.WriteLine($"[CSVaJSON] Primera estación:");
                Debug.WriteLine($"  Nombre: {g.NombreEstacion}");
                Debug.WriteLine($"  Municipio: {g.Municipio}");
                Debug.WriteLine($"  Provincia: {g.Provincia}");
                Debug.WriteLine($"  CP: {g.CodigoPostal}");
            }

            // ← AQUÍ ESTÁ EL CAMBIO: usar diccionarios con claves originales
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

            var opcionesJson = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string jsonContent = JsonSerializer.Serialize(listaDiccionarios, opcionesJson);

            string outputDir = Path.Combine(baseDirectory, "ArchivosFuenteConvertidos");
            Directory.CreateDirectory(outputDir);
            string jsonPath = Path.Combine(outputDir, "Estacions_ITV.json");
            var utf8NoBom = new UTF8Encoding(false);
            File.WriteAllText(jsonPath, jsonContent, utf8NoBom);

            Debug.WriteLine($"[CSVaJSON] JSON generado correctamente con cabeceras originales:");
            Debug.WriteLine($"→ {jsonPath}");

            return jsonContent;
        }
    }
}