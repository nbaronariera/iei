using System.Diagnostics;
using System.IO;
using System.Text;
using UI.Helpers;
using UI.Parsers;
using UI.Parsers.ParsedObjects;

namespace UI.Wrappers
{
    public static class JSONaJSONConversor
    {
        static CoordenadasSelenium seleniumHelper = new CoordenadasSelenium();
        public static string Ejecutar()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string jsonPath = Path.Combine(baseDirectory, "Fuentes", "estacionesEntrega.json");

            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"No se encontró el JSON: {jsonPath}");

            // 1️ Leer JSON
            var csvParser = new JSONParser();
            csvParser.Load(jsonPath);
            var listaObjetos = csvParser.ParseList();

            // 2 Generar el JSON final con coordenadas (tu lógica original)
            string jsonContent = generateString(listaObjetos);

            // 3️ Guardar con UTF-8 sin BOM (opcional, para depuración)
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            string outputDir = Path.Combine(baseDirectory, "ArchivosFuenteConvertidos");
            Directory.CreateDirectory(outputDir);
            string outputJsonPath = Path.Combine(outputDir, "estaciones_modificado.json");
            File.WriteAllText(outputJsonPath, jsonContent, utf8NoBom);
            Debug.WriteLine($"[OK] JSON generado con acentos y formato:\n {outputJsonPath}");

            return jsonContent; // ← ¡DEVUELVE EL CONTENIDO JSON!
        }
        private static string generateString(List<JSONData> elementos)
        {
            string res = "[";

            for (int i = 0; i < elementos.Count; i++)
            {
                var elemento = elementos[i];

                // --- LÓGICA DE FILTRADO ---
                bool esEstacionFija = elemento.TIPO_ESTACION != null &&
                                      elemento.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase);

                if (esEstacionFija)
                {
                    // CASO A: Es Fija -> Usamos Selenium
                    // Buscamos por Dirección + Municipio
                    var coords = seleniumHelper.ObtenerCoordenadas(elemento.DIRECCION, elemento.MUNICIPIO);
                    elemento.Latitud = coords.Lat;
                    elemento.Longitud = coords.Lng;
                }
                else
                {
                    // CASO B: Es Móvil o Agrícola -> Ponemos 0 y NO usamos Selenium
                    elemento.Latitud = 0.0;
                    elemento.Longitud = 0.0;
                }

                // Añadimos al string JSON
                res += "{" + elemento.ToJSON() + "}";

                // Añadimos coma si no es el último elemento
                if (i < elementos.Count - 1)
                {
                    res += ",\n";
                }
                else
                {
                    res += "\n";
                }
            }

            res += "]";
            return res;
        }
    }
}
