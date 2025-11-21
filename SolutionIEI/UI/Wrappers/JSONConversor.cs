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
    public static class JSONConversor
    {
        public static string Ejecutar()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string csvPath = Path.Combine(baseDirectory, "Fuentes", "estaciones.json");

            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"No se encontró el JSON: {csvPath}");

            // 1️ Leer JSON
            var csvParser = new JSONParser();
            csvParser.Load(csvPath);
            var listaObjetos = csvParser.ParseList(); // lista de objetos

            // TODO: Obtener coordenadas de Selenium y añadir lat y lon


            // 3 Guardar con UTF-8 sin BOM
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            string outputDir = Path.Combine(baseDirectory, "ArchivosFuenteConvertidos");
            Directory.CreateDirectory(outputDir);

            string jsonPath = Path.Combine(outputDir, "estaciones_modificado.json");
            File.WriteAllText(jsonPath, listaObjetos.ToString(), utf8NoBom);

            Debug.WriteLine($"[OK] JSON generado con acentos y formato:\n    {jsonPath}");

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
