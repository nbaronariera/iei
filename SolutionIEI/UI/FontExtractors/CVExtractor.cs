using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UI.Entidades;
using UI.Helpers;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    public class CVExtractor : Parser<JSONData>
    {

        private static readonly HashSet<string> territoriosValidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "Castellón", "Valencia", "Alicante"
        };

        private static readonly Dictionary<string, string> prefijosCpPorTerritorio = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Castellón", "12" },
            { "Valencia", "46" },
            { "Alicante", "03" },
        };

        protected override List<JSONData> ExecuteParse()
        {
            if (file == null) return new List<JSONData>();
            string contenido = new StreamReader(file, Encoding.UTF8).ReadToEnd();
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<JSONData>>(contenido, opciones) ?? new List<JSONData>();
        }

        public (List<ResultObject>, int, int) FromParsedToUsefull(List<JSONData> datosParseados)
        {
            var resultados = new List<ResultObject>();
            using var contexto = new AppDbContext();
            var debugResultados = new List<ResultadoDebug>();
            int noValidas = datosParseados.Count;
            int validas = 0;

            foreach (var dato in datosParseados)
            {
                var resultadoDebug = new ResultadoDebug
                {
                    Nombre = dato.MUNICIPIO?.Trim() ?? "(sin nombre)",
                    Provincia = dato.PROVINCIA?.Trim() ?? "",
                    Municipio = dato.MUNICIPIO?.Trim() ?? "",
                    CodigoPostal = dato.C_POSTAL?.Trim() ?? "",
                    Motivos = new List<string>()
                }; 

                try
                {
                    if (string.IsNullOrWhiteSpace(dato.MUNICIPIO))
                    {
                        resultadoDebug.Motivos.Add("Nombre estación vacío o nulo.");
                    }

                    if (string.IsNullOrWhiteSpace(dato.PROVINCIA))
                        resultadoDebug.Motivos.Add("Provincia vacía o nula.");
                    if (string.IsNullOrWhiteSpace(dato.MUNICIPIO))
                        resultadoDebug.Motivos.Add("Municipio vacío o nulo.");

                    string cpRaw = dato.C_POSTAL?.Trim() ?? "";

                    if (!Regex.IsMatch(cpRaw, @"^\d{5}$"))
                    {
                        resultadoDebug.Motivos.Add($"Código postal inválido ('{dato.C_POSTAL}'), al no tener 5 caracteres.");
                    }

                    var provincia = ObtenerOCrearProvincia(contexto, dato.PROVINCIA);
                    var localidad = ObtenerOCrearLocalidad(contexto, dato.MUNICIPIO, provincia);

                    if (string.IsNullOrWhiteSpace(provincia.nombre))
                    {
                        resultadoDebug.Motivos.Add($"Código postal '{cpRaw}' no corresponde con ninguna provincia conocida.");
                    }

                    resultadoDebug.Provincia = provincia.nombre;

                    double? lat = dato.Latitud, lon = dato.Longitud;

                    TipoEstacion tipo = TipoEstacion.Estacion_fija;
                    if (dato.TIPO_ESTACION.Contains("Móvil", StringComparison.OrdinalIgnoreCase)) tipo = TipoEstacion.Estacion_movil;
                    else if (dato.TIPO_ESTACION.Contains("Agrícola", StringComparison.OrdinalIgnoreCase)) tipo = TipoEstacion.Otros;


                    if (EstacionYaExiste(contexto, dato.Nº_ESTACION, lat.Value, lon.Value))
                    {
                        Debug.WriteLine($"[CAT] Estación duplicada omitida: {dato.Nº_ESTACION}");
                        continue;
                    }

                    string correoLimpio = EsCorreo(dato.CORREOS) ? dato.CORREOS : "";

                    if (resultadoDebug.Motivos.Count > 0)
                    {
                        resultadoDebug.Añadida = false;
                        debugResultados.Add(resultadoDebug);
                        continue;
                    }

                    var horario = dato.HORARIOS;
                    if (dato.TIPO_ESTACION == "Estación Fija")
                    {
                        horario = ConvertirFormatoFecha(dato.HORARIOS);
                    }

                    var estacion = new Estacion
                    {
                        nombre = string.IsNullOrWhiteSpace(dato.MUNICIPIO) ? dato.DIRECCION : dato.MUNICIPIO,
                        tipo = tipo,
                        direccion = dato.DIRECCION,
                        codigoPostal = dato.C_POSTAL,
                        latitud = lat.Value,
                        longitud = lon.Value,
                        descripcion = "",
                        horario = horario,
                        contacto = $"Correo electrónico: {dato.CORREOS} Teléfono: ",
                        URL = "https://www.sitval.com/",
                        localidad = localidad,
                        codigoLocalidad = localidad.codigo
                    };

                    contexto.Estaciones.Add(estacion);
                    resultados.Add(new ResultObject { Estacion = estacion, Localidad = localidad, Provincia = provincia });
                    validas++;
                    noValidas--;
                }
                catch { }
            }
            contexto.SaveChanges();
            MostrarResumen(debugResultados);
            return (resultados, validas, noValidas);
        }
        private bool EstacionYaExiste(AppDbContext ctx, string nombre, double lat, double lon)
        {
            return ctx.Estaciones.Any(e => e.nombre == nombre && e.latitud == lat && e.longitud == lon);
        }
        private bool EsCorreo(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            return Regex.IsMatch(texto, @"^[^\s@]+@[^\s@]+\.[^\s@]+$");
        }

        private Provincia ObtenerOCrearProvincia(AppDbContext ctx, string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) nombre = "Desconocida";
            var ex = ctx.Provincias.FirstOrDefault(p => p.nombre == nombre);
            if (ex != null) return ex;
            var n = new Provincia(nombre); ctx.Provincias.Add(n); ctx.SaveChanges(); return n;
        }

        private Localidad ObtenerOCrearLocalidad(AppDbContext ctx, string nombre, Provincia prov)
        {
            if (string.IsNullOrWhiteSpace(nombre)) nombre = "Desconocida";
            var ex = ctx.Localidades.FirstOrDefault(l => l.nombre == nombre && l.codigoProvincia == prov.codigo);
            if (ex != null) return ex;
            var n = new Localidad(nombre) { Provincia = prov, codigoProvincia = prov.codigo };
            ctx.Localidades.Add(n); ctx.SaveChanges(); return n;
        }
        private void MostrarResumen(List<ResultadoDebug> resultados)
        {
            var añadidas = resultados.Where(r => r.Añadida).ToList();
            var descartadas = resultados.Where(r => !r.Añadida).ToList();

            Debug.WriteLine("\n ESTACIONES AÑADIDAS");
            Debug.WriteLine("------------------------------------------------------------");
            Debug.WriteLine($"{"Nombre",-35} | {"Provincia",-12} | {"Municipio",-18} | {"CP",-6} | {"Motivos"}");
            Debug.WriteLine("------------------------------------------------------------");
            foreach (var r in añadidas)
                Debug.WriteLine($"{r.Nombre,-35} | {r.Provincia,-12} | {r.Municipio,-18} | {r.CodigoPostal,-6} | {string.Join("; ", r.Motivos)}");

            Debug.WriteLine("\n ESTACIONES DESCARTADAS");
            Debug.WriteLine("------------------------------------------------------------");
            Debug.WriteLine($"{"Nombre",-35} | {"Provincia",-12} | {"Municipio",-18} | {"CP",-6} | {"Motivos"}");
            Debug.WriteLine("------------------------------------------------------------");
            foreach (var r in descartadas)
                Debug.WriteLine($"{r.Nombre,-35} | {r.Provincia,-12} | {r.Municipio,-18} | {r.CodigoPostal,-6} | {string.Join("; ", r.Motivos)}");

            Debug.WriteLine($"\n Total añadidas: {añadidas.Count}, descartadas: {descartadas.Count}");
        }
        private string ConvertirFormatoFecha(string input)
        {
            // Expresión regular para detectar días de la semana y sus horarios
            string pattern = @"([A-Z]+)\.\s*(\d{1,2}:\d{2}-\d{1,2}:\d{2})";
            var matches = Regex.Matches(input, pattern);

            string result = "";

            foreach (Match match in matches)
            {
                string dia = match.Groups[1].Value;
                string horarios = match.Groups[2].Value;

                var diaFormat = dia.Replace(".", "");
                if (diaFormat.Length == 2)
                {
                    diaFormat.Insert(1, "-");
                }

                result += $"{dia} ({horarios}) ";
            }

            // Eliminar el último espacio extra
            return result.Trim();
        }
    }
}