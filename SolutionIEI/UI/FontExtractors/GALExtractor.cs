using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UI.Entidades;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    class ResultadoDebug
    {
        public string Nombre { get; set; } = "";
        public string Provincia { get; set; } = "";
        public string Municipio { get; set; } = "";
        public string CodigoPostal { get; set; } = "";
        public bool Añadida { get; set; }
        public List<string> Motivos { get; set; } = new();
    }

    public class GALExtractor : Parser<GALData>
    {
        private static readonly Dictionary<string, int> provinciasGallegas = new(StringComparer.OrdinalIgnoreCase)
        {
            {"A Coruña", 15},
            {"Lugo", 27},
            {"Ourense", 32},
            {"Pontevedra", 36}
        };

        List<string> provincias = new List<string> { "A Coruña", "Pontevedra", "Lugo", "Ourense" };

        private int codigoPostal;

        protected override List<GALData> ExecuteParse()
        {
            if (file == null) return new List<GALData>();

            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            string contenido = new StreamReader(file, Encoding.UTF8).ReadToEnd();
            return JsonSerializer.Deserialize<List<GALData>>(contenido, opciones) ?? new List<GALData>();
        }

        public (List<ResultObject>, int, int) FromParsedToUsefull(List<GALData> datosParseados)
        {
            var resultados = new List<ResultObject>();
            using var contexto = new AppDbContext();
            var debugResultados = new List<ResultadoDebug>();
            int noValidas = datosParseados.Count;
            int validas = 0;

            Debug.WriteLine($" Iniciando parseo de {datosParseados.Count} registros GAL.");

            foreach (var dato in datosParseados)
            {
                var resultadoDebug = new ResultadoDebug
                {
                    Nombre = dato?.NombreEstacion ?? "(sin nombre)",
                    Provincia = dato?.Provincia ?? "",
                    Municipio = dato?.Municipio ?? "",
                    CodigoPostal = dato?.CodigoPostal ?? "",
                    Motivos = new List<string>()
                };

                if (string.IsNullOrWhiteSpace(dato.NombreEstacion))
                {
                    resultadoDebug.Motivos.Add("Nombre de estación vacío o nulo.");
                }

                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(dato.Provincia))
                    resultadoDebug.Motivos.Add("Provincia vacía o nula.");
                if (string.IsNullOrWhiteSpace(dato.Municipio))
                    resultadoDebug.Motivos.Add("Municipio vacío o nulo.");

                if (!int.TryParse(dato.CodigoPostal, out codigoPostal) || codigoPostal < 10000 || codigoPostal > 99999)
                    resultadoDebug.Motivos.Add($"Código postal inválido ('{dato.CodigoPostal}').");

                if (!provincias.Contains(dato.Provincia.Trim()))
                {
                    resultadoDebug.Motivos.Add("Provincia no válida.");
                }

                else if (!CodigoPostalValido(codigoPostal, dato.Provincia))
                    resultadoDebug.Motivos.Add($"Código postal {codigoPostal} no coincide con provincia '{dato.Provincia}'.");

                double lat = ExtraerLatitud(dato.Coordenadas);
                double lon = ExtraerLongitud(dato.Coordenadas);

                if (!EsCoordenadaEnEspañaPeninsular(lat, lon))
                    resultadoDebug.Motivos.Add($"Coordenadas fuera de España peninsular ({lat}, {lon}).");

                if (EstacionYaExiste(contexto, dato.NombreEstacion, lat, lon))
                {
                    resultadoDebug.Motivos.Add("Estación duplicada.");
                    
                }

               

                if (resultadoDebug.Motivos.Count > 0)
                {
                    resultadoDebug.Añadida = false;
                    debugResultados.Add(resultadoDebug);
                    continue;
                }

                resultadoDebug.Añadida = true;
                validas++;
                noValidas--;

                // Obtener o crear provincia y localidad de forma segura
                var provincia = ObtenerOCrearProvincia(contexto, dato.Provincia);
                var localidad = ObtenerOCrearLocalidad(contexto, dato.Municipio, provincia);

                var estacion = new Estacion
                {
                    nombre = dato.NombreEstacion,
                    tipo = TipoEstacion.Estacion_fija,
                    direccion = dato.Direccion,
                    codigoPostal = dato.CodigoPostal,
                    latitud = lat,
                    longitud = lon,
                    descripcion = "",
                    horario = ConvertirHorario(dato.HorarioRaw),
                    contacto = FormatearContacto(dato.Correo, dato.Telefono),
                    URL = dato.UrlCita,
                    localidad = localidad,
                    codigoLocalidad = localidad.codigo
                };

                contexto.Estaciones.Add(estacion);
                resultados.Add(new ResultObject
                {
                    Estacion = estacion,
                    Localidad = localidad,
                    Provincia = provincia
                });

                debugResultados.Add(resultadoDebug);
            }

            contexto.SaveChanges();

            MostrarResumen(debugResultados);
            return (resultados, validas, noValidas);
        }

        private Provincia ObtenerOCrearProvincia(AppDbContext ctx, string nombre)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));

            if (string.IsNullOrWhiteSpace(nombre))
                nombre = "Provincia desconocida";

            nombre = nombre.Trim();

            // Búsqueda case-insensitive (se traduce a SQL en EF Core)
            var existente = ctx.Provincias
                .FirstOrDefault(p => p.nombre.ToLower() == nombre.ToLower());

            if (existente != null) return existente;

            // Crear nueva provincia y guardar para obtener el código autogenerado
            var nueva = new Provincia(nombre);
            ctx.Provincias.Add(nueva);
            ctx.SaveChanges(); // Guardamos para que nueva.codigo tenga valor y podamos usarla como FK
            return nueva;
        }

        private Localidad ObtenerOCrearLocalidad(AppDbContext ctx, string nombre, Provincia provincia)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (provincia == null) throw new ArgumentNullException(nameof(provincia));
            if (string.IsNullOrWhiteSpace(nombre)) nombre = "Localidad desconocida";

            nombre = nombre.Trim();

            var existente = ctx.Localidades
                .FirstOrDefault(l => l.nombre.ToLower() == nombre.ToLower() && l.codigoProvincia == provincia.codigo);

            if (existente != null) return existente;

            var nueva = new Localidad(nombre)
            {
                Provincia = provincia,
                codigoProvincia = provincia.codigo // provincia.codigo ya existe porque guardamos antes
            };

            ctx.Localidades.Add(nueva);
            ctx.SaveChanges(); // Guardamos para obtener nueva.codigo
            return nueva;
        }

        // --- Métodos auxiliares ---

        private double ExtraerLatitud(string coordenadas)
        {
            if (string.IsNullOrWhiteSpace(coordenadas)) return 0;
            var partes = coordenadas.Split(',');
            return ParsearCoordenadaDMS(partes[0].Trim());
        }

        private double ExtraerLongitud(string coordenadas)
        {
            if (string.IsNullOrWhiteSpace(coordenadas)) return 0;
            var partes = coordenadas.Split(',');
            if (partes.Length < 2) return 0;
            return ParsearCoordenadaDMS(partes[1].Trim());
        }

        private double ParsearCoordenadaDMS(string dms)
        {
            if (string.IsNullOrWhiteSpace(dms)) return 0;

            // Normalizar símbolos
            dms = dms
                .Replace("º", "°")
                .Replace("�", "°")
                .Replace("’", "'")
                .Replace("′", "'")
                .Replace("“", "\"")
                .Replace("”", "\"")
                .Replace(",", ".")   // cambiar coma decimal por punto
                .Trim();

            // 1) Si ya es decimal puro
            if (double.TryParse(dms, NumberStyles.Any, CultureInfo.InvariantCulture, out double dec))
                return dec;

            // 2) Detectar patrón tipo DMS (43° 18.856')
            var match = Regex.Match(dms, @"(-?\d+(?:\.\d*)?)\s*°\s*(\d+(?:\.\d*)?)?");

            if (match.Success)
            {
                double grados = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                double minutos = match.Groups[2].Success
                    ? double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
                    : 0;

                double signo = grados < 0 ? -1 : 1;
                grados = Math.Abs(grados);

                return signo * (grados + minutos / 60.0);
            }

            // 3) Si nada coincide
            return 0;
        }

        private bool CodigoPostalValido(int codigo, string provincia)
        {
            if (string.IsNullOrWhiteSpace(provincia)) return false;
            return provinciasGallegas.TryGetValue(provincia.Trim(), out int cp) && (codigo / 1000) == cp;
        }

        private bool EsCoordenadaEnEspañaPeninsular(double lat, double lon)
        {
            const double latMin = 36.0, latMax = 43.8;
            const double lonMin = -9.3, lonMax = 3.3;
            return lat >= latMin && lat <= latMax && lon >= lonMin && lon <= lonMax;
        }

        private string ConvertirHorario(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            raw = raw.ToLower()
                     .Replace(" horas", "")
                     .Replace(" h.", "")
                     .Replace(".", ":");


            var diasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "de luns a venres", "L-V" },
        { "sábados",          "S"   }
    };

            // Buscar bloques "(de luns a venres)" y "(sábados)"
            var bloques = Regex.Matches(raw, @"(?<rangos>.*?)\((?<dias>.*?)\)")
                               .Cast<Match>()
                               .Select(m => new
                               {
                                   RangosRaw = m.Groups["rangos"].Value,
                                   DiasRaw = m.Groups["dias"].Value.Trim()
                               })
                               .ToList();

            if (!bloques.Any())
                return "";

            var resultadoFinal = new List<string>();

            foreach (var bloque in bloques)
            {
                // Determinar etiqueta de días (L-V, S…)
                string etiquetaDias = diasMap
                    .Where(kv => bloque.DiasRaw.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                    .Select(kv => kv.Value)
                    .FirstOrDefault() ?? bloque.DiasRaw;

                // Extraer todos los rangos horarios del bloque
                // Ej: "de 8:30 a 14:00 e de 16:00 a 19:30"
                var rangos = Regex.Matches(bloque.RangosRaw,
                                           @"(\d{1,2}[:]\d{1,2})\s*a\s*(\d{1,2}[:]\d{1,2})",
                                           RegexOptions.IgnoreCase)
                                  .Cast<Match>()
                                  .Select(m => $"{m.Groups[1].Value}-{m.Groups[2].Value}")
                                  .ToList();

                if (rangos.Count > 0)
                {
                    resultadoFinal.Add($"{etiquetaDias} ({string.Join(",", rangos)})");
                }
            }

            return string.Join(" ", resultadoFinal);
        }

        private string NormalizarHora(string h)
        {
            h = h.Replace(".", ":");
            if (!h.Contains(":")) h += ":00";

            // Convertir 8:0 a 8:00
            var parts = h.Split(':');
            if (parts.Length == 2 && parts[1].Length == 1)
                return $"{parts[0]}:0{parts[1]}";

            return h;
        }

        private bool EstacionYaExiste(AppDbContext ctx, string nombre, double lat, double lon)
        {
            string nombreNorm = nombre.Trim().ToLower();
            double latNorm = Math.Round(lat, 6);
            double lonNorm = Math.Round(lon, 6);

            return ctx.Estaciones.Any(e =>
                // Coincide el nombre
                e.nombre.Trim().ToLower() == nombreNorm ||

                // Coinciden las coordenadas
                (Math.Round(e.latitud, 6) == latNorm &&
                 Math.Round(e.longitud, 6) == lonNorm)
            );
        }

        private string FormatearContacto(string correo, string telefono) => $"Correo electrónico: {correo} Teléfono: {telefono}";

        private void MostrarResumen(List<ResultadoDebug> resultados)
        {
            var añadidas = resultados.Where(r => r.Añadida).ToList();
            var descartadas = resultados.Where(r => !r.Añadida).ToList();

            Debug.WriteLine("\n ESTACIONES AÑADIDAS");
            Debug.WriteLine("------------------------------------------------------------");
            Debug.WriteLine($"{"Nombre",-35} | {"Provincia",-12} | {"CP",-6} | {"Motivos"}");
            Debug.WriteLine("------------------------------------------------------------");
            foreach (var r in añadidas)
                Debug.WriteLine($"{r.Nombre,-35} | {r.Provincia,-12} | {r.CodigoPostal,-6} | {string.Join("; ", r.Motivos)}");

            Debug.WriteLine("\n ESTACIONES DESCARTADAS");
            Debug.WriteLine("------------------------------------------------------------");
            Debug.WriteLine($"{"Nombre",-35} | {"Provincia",-12} | {"CP",-6} | {"Motivos"}");
            Debug.WriteLine("------------------------------------------------------------");
            foreach (var r in descartadas)
                Debug.WriteLine($"{r.Nombre,-35} | {r.Provincia,-12} | {r.CodigoPostal,-6} | {string.Join("; ", r.Motivos)}");

            Debug.WriteLine($"\n Total añadidas: {añadidas.Count}, descartadas: {descartadas.Count}");
        }
    }
}