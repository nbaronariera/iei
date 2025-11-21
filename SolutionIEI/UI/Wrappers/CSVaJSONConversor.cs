using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Collections.Generic;
using UI.Parsers;

namespace UI.Wrappers
{
    public static class CSVaJSONConversor
    {
        public static string Ejecutar()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string csvPath = Path.Combine(baseDirectory, "Fuentes", "Estacions_ITV.csv");

            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"No se encontró el CSV: {csvPath}");

            // 1️ Leer CSV
            var csvParser = new CSVParser();
            csvParser.Load(csvPath);
            var listaObjetos = csvParser.ParseList(); // lista de objetos 'gal'

            // 2️ Convertir a lista de diccionarios respetando nombres del CSV
            var listaDiccionarios = new List<Dictionary<string, string>>();

            foreach (var fila in listaObjetos)
            {
                var dict = new Dictionary<string, string>
                {
                    ["NOME DA ESTACIÓN"] = fila.NombreEstacion,
                    ["ENDEREZO"] = fila.Direccion,
                    ["CONCELLO"] = fila.Municipio,
                    ["CÓDIGO POSTAL"] = fila.CodigoPostal,
                    ["PROVINCIA"] = fila.Provincia,
                    ["TELÉFONO"] = fila.Telefono,
                    ["HORARIO"] = fila.HorarioRaw,
                    ["SOLICITUDE DE CITA PREVIA"] = fila.UrlCita,
                    ["CORREO ELECTRÓNICO"] = fila.Correo,
                    ["COORDENADAS GMAPS"] = fila.Coordenadas
                };
                listaDiccionarios.Add(dict);
            }

            // 3️ Convertir a JSON (acentos + formateado)
            var opcionesJson = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string jsonContent = JsonSerializer.Serialize(listaDiccionarios, opcionesJson);

            // 4️ Guardar con UTF-8 sin BOM
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            string outputDir = Path.Combine(baseDirectory, "ArchivosFuenteConvertidos");
            Directory.CreateDirectory(outputDir);

            string jsonPath = Path.Combine(outputDir, "Estacions_ITV.json");
            File.WriteAllText(jsonPath, jsonContent, utf8NoBom);

            Debug.WriteLine($"[OK] JSON generado con acentos y formato:\n    {jsonPath}");

            // 5️ Abrir carpeta automáticamente
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
