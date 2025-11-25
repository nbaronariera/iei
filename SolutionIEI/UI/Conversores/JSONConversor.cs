using System.Diagnostics;
using System.Text;
using UI.Helpers;
using UI.Parsers;
using UI.Parsers.ParsedObjects;

namespace UI.Wrappers
{
    public static class JSONConversor
    {
        
        static CoordenadasSelenium seleniumHelper = new CoordenadasSelenium();
        public static string Ejecutar()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string csvPath = Path.Combine(baseDirectory, "Fuentes", "estacionesEntrega.json");

            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"No se encontró el JSON: {csvPath}");

            // 1️ Leer JSON
            var csvParser = new JSONParser();
            csvParser.Load(csvPath);
            var listaObjetos = csvParser.ParseList();

            // 2 Guardar con UTF-8 sin BOM
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            string outputDir = Path.Combine(baseDirectory, "ArchivosFuenteConvertidos");
            Directory.CreateDirectory(outputDir);

            string jsonPath = Path.Combine(outputDir, "estaciones_modificado.json");
            File.WriteAllText(jsonPath, generateString(listaObjetos), utf8NoBom);

            Debug.WriteLine($"[OK] JSON generado con acentos y formato:\n    {jsonPath}");

            // 3 Abrir carpeta automáticamente
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
        private static string generateString(List<JSONData> elementos)
        {
            string res = "[";

            foreach(var elemento in elementos){
                string direccionBusqueda = elemento.DIRECCION;
                if (direccionBusqueda.Contains("I.T.V. Móvil") || direccionBusqueda.Contains("I.T.V. Agrícola"))
                {
                    direccionBusqueda = elemento.MUNICIPIO;
                }

                var coords = seleniumHelper.ObtenerCoordenadas(direccionBusqueda, elemento.MUNICIPIO);
                elemento.Latitud = coords.Lat;
                elemento.Longitud = coords.Lng;
                res += "{" + elemento.ToJSON() + "},\n"; 
            }

            res += "]";
            return res;
        }
    }
}
