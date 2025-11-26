using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UI.Entidades;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    public class CATExtractor : Parser<XMLData>
    {
        private int codigoPostal;

        private static readonly HashSet<string> territoriosValidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "Barcelona", "Tarragona", "Lleida", "Girona"
        };

        private static readonly Dictionary<string, string> prefijosCpPorTerritorio = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Lleida", "25" },
            { "Barcelona", "08" },
            { "Girona", "17" },
            { "Tarragona", "43" }
        };

        protected override List<XMLData> ExecuteParse()
        {
            if (file == null) return new List<XMLData>();

            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            string contenido = new StreamReader(file, Encoding.UTF8).ReadToEnd();

            return JsonSerializer.Deserialize<List<XMLData>>(contenido, opciones) ?? new List<XMLData>();
        }

        public (List<ResultObject>, int, int) FromParsedToUsefull(List<XMLData> datosParseados)
        {
            var resultados = new List<ResultObject>();
            using var contexto = new AppDbContext();
            var debugResultados = new List<ResultadoDebug>();
            int noValidas = datosParseados.Count;
            int validas = 0;

            Debug.WriteLine($"[CAT] Iniciando procesamiento de {datosParseados.Count} registros CAT.");

            foreach (var dato in datosParseados)
            {
                var resultadoDebug = new ResultadoDebug
                {
                    Nombre = dato.denominaci?.Trim() ?? "(sin nombre)",
                    Provincia = dato.serveis_territorials?.Trim() ?? "",
                    Municipio = dato.municipi?.Trim() ?? "",
                    CodigoPostal = dato.cp?.Trim() ?? "",
                    Motivos = new List<string>()
                };

                try
                {
                    if (string.IsNullOrWhiteSpace(dato.denominaci))
                    {
                        resultadoDebug.Motivos.Add("Nombre estación vacío o nulo.");
                    }

                    if (string.IsNullOrWhiteSpace(dato.serveis_territorials))
                        resultadoDebug.Motivos.Add("Provincia vacía o nula.");
                    if (string.IsNullOrWhiteSpace(dato.municipi))
                        resultadoDebug.Motivos.Add("Municipio vacío o nulo.");

                    string cpRaw = dato.cp?.Trim() ?? "";

                    if (!Regex.IsMatch(cpRaw, @"^\d{5}$"))
                    {
                        resultadoDebug.Motivos.Add($"Código postal inválido ('{dato.cp}'), al no tener 5 caracteres.");
                    }

                    // Determinar provincia usando la nueva lógica
                    string provinciaNombre = ObtenerProvinciaPorCodigoPostal(cpRaw, resultadoDebug.Motivos);

                    if (string.IsNullOrWhiteSpace(provinciaNombre))
                    {
                        resultadoDebug.Motivos.Add($"Código postal '{cpRaw}' no corresponde con ninguna provincia conocida.");
                    }

                    resultadoDebug.Provincia = provinciaNombre;

                    double lat = 0.0;
                    double lon = 0.0;

                    if (dato.localitzador_a_google_maps?.url != null)
                    {
                        var match = Regex.Match(dato.localitzador_a_google_maps.url, @"q=([\d\.]+)\+([\d\.]+)");
                        if (match.Success)
                        {
                            lat = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                            lon = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            resultadoDebug.Motivos.Add("No se pudieron extraer coordenadas de Google Maps.");
                        }
                    }
                    else
                    {
                        // Fallback antiguo (si no hay URL de Google Maps)
                        lat = ParsearCoordenada(dato.lat);
                        lon = ParsearCoordenada(dato.long_coord);
                    }

                    if (EstacionYaExiste(contexto, dato.denominaci, lat, lon))
                    {
                        resultadoDebug.Motivos.Add("Estación duplicada.");
                    }

                    if (!EsCoordenadaEnEspañaPeninsular(lat, lon))
                        resultadoDebug.Motivos.Add($"Coordenadas fuera de España peninsular ({lat}, {lon}).");

                    string correoLimpio = EsUrl(dato.correu_electr_nic) ? "" : dato.correu_electr_nic;
                    string contactoFormateado = $"Correo electrónico: {correoLimpio} Teléfono: {dato.tel_atenc_public}";

                    if (resultadoDebug.Motivos.Count > 0)
                    {
                        resultadoDebug.Añadida = false;
                        resultadoDebug.Provincia = dato.serveis_territorials?.Trim() ?? "(desconocida)";
                        debugResultados.Add(resultadoDebug);
                        continue;
                    }

                    resultadoDebug.Añadida = true;
                    validas++;
                    noValidas--;

                    // Obtener o crear provincia y localidad de forma segura
                    var provincia = ObtenerOCrearProvincia(contexto, provinciaNombre);
                    var localidad = ObtenerOCrearLocalidad(contexto, dato.municipi, provincia);

                    // Creación de la entidad
                    var estacion = new Estacion
                    {
                        nombre = dato.denominaci,
                        tipo = TipoEstacion.Estacion_fija, // Valor fijo según PDF
                        direccion = dato.adre_a ?? "",
                        codigoPostal = dato.cp,
                        latitud = lat,
                        longitud = lon,
                        descripcion = "",
                        horario = ConvertirHorarioCAT(dato.horari_de_servei),
                        contacto = contactoFormateado,
                        URL = dato.web?.url ?? "",
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
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CAT] Error procesando estación {dato.denominaci}: {ex.Message}");
                }
            }

            contexto.SaveChanges();
            MostrarResumen(debugResultados);
            Debug.WriteLine($"[CAT] Carga finalizada. {resultados.Count} estaciones guardadas.");
            return (resultados, validas, noValidas);
        }

        private string ObtenerProvinciaPorCodigoPostal(string cp, List<string> motivos)
        {
            if (string.IsNullOrWhiteSpace(cp) || cp.Length != 5)
                return "";

            string prefijo = cp.Substring(0, 2);

            return prefijo switch
            {
                "08" => "Barcelona",
                "17" => "Girona",
                "25" => "Lleida",
                "43" => "Tarragona",
                _ => ""
            };
        }

        private bool EsCoordenadaEnEspañaPeninsular(double lat, double lon)
        {
            const double latMin = 36.0, latMax = 43.8;
            const double lonMin = -9.3, lonMax = 3.3;
            return lat >= latMin && lat <= latMax && lon >= lonMin && lon <= lonMax;
        }

        private Provincia ObtenerOCrearProvincia(AppDbContext ctx, string nombre)
        {
            nombre = nombre.Trim();
            var existente = ctx.Provincias.FirstOrDefault(p => p.nombre.ToLower() == nombre.ToLower());
            if (existente != null) return existente;

            var nueva = new Provincia(nombre);
            ctx.Provincias.Add(nueva);
            ctx.SaveChanges();
            return nueva;
        }

        private Localidad ObtenerOCrearLocalidad(AppDbContext ctx, string nombre, Provincia provincia)
        {
            nombre = nombre.Trim();
            var existente = ctx.Localidades.FirstOrDefault(l => l.nombre.ToLower() == nombre.ToLower() && l.codigoProvincia == provincia.codigo);
            if (existente != null) return existente;

            var nueva = new Localidad(nombre) { Provincia = provincia, codigoProvincia = provincia.codigo };
            ctx.Localidades.Add(nueva);
            ctx.SaveChanges();
            return nueva;
        }

        private bool EstacionYaExiste(AppDbContext ctx, string nombre, double lat, double lon)
        {
            return ctx.Estaciones.Any(e => e.nombre == nombre && e.latitud == lat && e.longitud == lon);
        }

        private double ParsearCoordenada(string coord)
        {
            if (string.IsNullOrWhiteSpace(coord)) return 0.0;
            string s = Regex.Replace(coord, @"[^\d]", "");

            if (!long.TryParse(s, out long n)) return 0.0;

            if (s.Length == 8) return n / 1_000_000.0;

            if (s.Length == 6 || s.Length == 7) return n / 1_000_000.0;

            // fallback

            return n / 1_000_000.0;
        }

        private bool EsUrl(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            return texto.ToLower().StartsWith("http") || texto.ToLower().StartsWith("www");
        }


        private string ConvertirHorarioCAT(string raw)
        {
            /*
            if (string.IsNullOrWhiteSpace(raw)) return "";


            string horario = raw;

            horario = Regex.Replace(horario, "De dilluns a divendres", "L-V", RegexOptions.IgnoreCase);
            horario = Regex.Replace(horario, "Dilluns a divendres", "L-V", RegexOptions.IgnoreCase);
            horario = Regex.Replace(horario, "dissabtes?", "S", RegexOptions.IgnoreCase);
            horario = Regex.Replace(horario, "diumenges?", "D", RegexOptions.IgnoreCase);

            horario = horario.Replace(" h.", "").Replace(" h ", " ");

            return horario.Trim();
            */
            return raw;
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
    }
}