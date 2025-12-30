using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UI.Entidades;
using UI.Parsers.ParsedObjects;
using UI.Wrappers;

namespace UI.Parsers
{
    public class CATExtractor : Parser<XMLData>
    {
        private int codigoPostal;
        HttpClient _http = new HttpClient { 
            BaseAddress = new Uri("http://localhost:8083"),
            Timeout = Timeout.InfiniteTimeSpan
        };

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

        public (List<ResultObject>, int, String, String) FromParsedToUsefull(List<XMLData> datosParseados)
        {
            var resultados = new List<ResultObject>();
            var estacionesValidas = new List<Estacion>();
            using var contexto = new AppDbContext();
            var debugResultados = new List<ResultadoDebug>();

            var nombresEnEsteFichero = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var coordenadasEnEsteFichero = new HashSet<string>();

            Console.WriteLine($"[CAT] Iniciando procesamiento de {datosParseados.Count} registros CAT.");

            foreach (var dato in datosParseados)
            {
                var resultadoDebug = new ResultadoDebug
                {
                    Nombre = dato.denominaci?.Trim() ?? "(sin nombre)",
                    Provincia = dato.serveis_territorials?.Trim() ?? "",
                    Municipio = dato.municipi?.Trim() ?? "",
                    CodigoPostal = dato.cp?.Trim() ?? "",
                    Motivos = new List<string>(),
                    Añadida = true
                };
                resultadoDebug.Fuente = "CAT";

                try
                {
                    if (string.IsNullOrWhiteSpace(dato.denominaci))
                    {
                        resultadoDebug.Motivos.Add("Nombre estación vacío o nulo.");
                        resultadoDebug.Añadida = false;
                    }
                    if (string.IsNullOrWhiteSpace(dato.serveis_territorials))
                    {
                        resultadoDebug.Motivos.Add("Provincia vacía o nula.");
                        resultadoDebug.Añadida = false;
                    }
                    if (string.IsNullOrWhiteSpace(dato.municipi))
                    {
                        resultadoDebug.Motivos.Add("Municipio vacío o nulo.");
                        resultadoDebug.Añadida = false;
                    }

                    string cpRaw = dato.cp?.Trim() ?? "";
                    if (!Regex.IsMatch(cpRaw, @"^\d{5}$"))
                    {
                        resultadoDebug.Motivos.Add($"Código postal inválido ('{dato.cp}'), al no tener 5 caracteres.");
                        resultadoDebug.Añadida = false;
                    }

                    // Provincia según código postal (siempre la referencia correcta)
                    string provinciaPorCP = ObtenerProvinciaPorCodigoPostal(cpRaw, resultadoDebug.Motivos);
                    if (string.IsNullOrWhiteSpace(provinciaPorCP))
                    {
                        resultadoDebug.Motivos.Add($"Código postal '{cpRaw}' no corresponde con ninguna provincia catalana.");
                        resultadoDebug.Añadida = false;
                    }

                    string provinciaCampoOriginal = dato.serveis_territorials?.Trim() ?? "";
                    string provinciaCampo = provinciaCampoOriginal;

                    // === Mapeo de variantes en castellano a catalán para la comprobación ===
                    var variantesCastellano = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Gerona", "Girona" },
                { "Lérida", "Lleida" }
                // Barcelona y Tarragona son iguales
            };

                    if (variantesCastellano.TryGetValue(provinciaCampoOriginal, out string provinciaCatalana))
                    {
                        provinciaCampo = provinciaCatalana;
                    }

                    // === Comprobación de coherencia: solo si es una provincia catalana principal (incluyendo variantes) ===
                    var provinciasPrincipales = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Barcelona", "Girona", "Lleida", "Tarragona"
            };

                    if (provinciasPrincipales.Contains(provinciaCampo) &&
                        !string.Equals(provinciaCampo, provinciaPorCP, StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Motivos.Add($"Código postal {cpRaw} no coincide con la provincia almacenada en el campo serveis_territorials ({provinciaCampoOriginal}).");
                        resultadoDebug.Añadida = false; // ← Se descarta directamente
                    }

                    // Si es "Terres de l'Ebre" o cualquier otro valor → no se comprueba coherencia
                    resultadoDebug.Provincia = provinciaPorCP; // Siempre usamos la del CP como definitiva

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
                            resultadoDebug.Añadida = false;
                        }
                    }
                    else
                    {
                        lat = ParsearCoordenada(dato.lat);
                        lon = ParsearCoordenada(dato.long_coord);
                    }

                    string nombreNormalizado = dato.denominaci?.Trim() ?? "";
                    string coordsKey = $"{lat:F6},{lon:F6}";

                    // Duplicados internos
                    if (!string.IsNullOrWhiteSpace(nombreNormalizado) && !nombresEnEsteFichero.Add(nombreNormalizado))
                    {
                        resultadoDebug.Motivos.Add($"Nombre repetido dentro del archivo CAT ({nombreNormalizado}).");
                        resultadoDebug.Añadida = false;
                    }
                    if (lat != 0 && lon != 0 && !coordenadasEnEsteFichero.Add(coordsKey))
                    {
                        resultadoDebug.Motivos.Add($"Coordenadas repetidas dentro del archivo CAT ({lat:F6}, {lon:F6}).");
                        resultadoDebug.Añadida = false;
                    }

                    // Duplicados en BD
                    if (contexto.Estaciones.Any(e => e.nombre.ToLower() == nombreNormalizado.ToLower()))
                    {
                        resultadoDebug.Motivos.Add($"Nombre ya existe en la base de datos ({nombreNormalizado}).");
                        resultadoDebug.Añadida = false;
                    }
                    if (lat != 0 && lon != 0 && contexto.Estaciones.Any(e =>
                        Math.Abs(e.latitud - lat) < 0.0001 && Math.Abs(e.longitud - lon) < 0.0001))
                    {
                        resultadoDebug.Motivos.Add($"Ubicación ya usada en la base de datos ({lat:F6}, {lon:F6}).");
                        resultadoDebug.Añadida = false;
                    }

                    if (!EsCoordenadaEnEspañaPeninsular(lat, lon))
                    {
                        resultadoDebug.Motivos.Add($"Coordenadas fuera de España peninsular ({lat}, {lon}).");
                        resultadoDebug.Añadida = false;
                    }

                    if (!resultadoDebug.Añadida)
                    {
                        debugResultados.Add(resultadoDebug);
                        continue;
                    }

                    // Todo válido → crear objetos
                    var provincia = ObtenerOCrearProvincia(contexto, provinciaPorCP);
                    var localidad = ObtenerOCrearLocalidad(contexto, dato.municipi, provincia);

                    string correoLimpio = EsUrl(dato.correu_electr_nic) ? "" : dato.correu_electr_nic;
                    string contactoFormateado = $"Correo electrónico: {correoLimpio} Teléfono: {dato.tel_atenc_public}";

                    var estacion = new Estacion
                    {
                        nombre = dato.denominaci,
                        tipo = TipoEstacion.Estacion_fija,
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

                    estacionesValidas.Add(estacion);
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
                    Console.WriteLine($"[CAT] Error procesando estación {dato.denominaci}: {ex.Message}");
                    resultadoDebug.Motivos.Add($"Excepción: {ex.Message}");
                    debugResultados.Add(resultadoDebug);
                }
            }

            if (estacionesValidas.Any())
            {
                contexto.Estaciones.AddRange(estacionesValidas);
                contexto.SaveChanges();
            }

            MostrarResumen(debugResultados);
            Console.WriteLine($"[CAT] Carga finalizada. {resultados.Count} estaciones guardadas.");

            int agregadas = resultados.Count;
            string estacionesReparadas = string.Join("\n",
                debugResultados.Where(r => r.Añadida && r.Reparada)
                    .Select(r => $"{{{r.Fuente}, {r.Nombre}, {r.Municipio}, [{string.Join("; ", r.Motivos)}], [{string.Join("; ", r.Reparaciones)}]}}"));

            string estacionesRechazadas = string.Join("\n",
                debugResultados.Where(r => !r.Añadida)
                    .Select(r => $"{{{r.Fuente}, {r.Nombre}, {r.Municipio}, [{string.Join("; ", r.Motivos)}]}}"));

            return (resultados, agregadas, estacionesReparadas, estacionesRechazadas);
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
            return raw;
        }

        private void MostrarResumen(List<ResultadoDebug> resultados)
        {
            var añadidas = resultados.Where(r => r.Añadida).ToList();
            var descartadas = resultados.Where(r => !r.Añadida).ToList();

            Console.WriteLine("\n ESTACIONES AÑADIDAS");
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine($"{"Nombre",-35} | {"Provincia",-12} | {"Municipio",-18} | {"CP",-6} | {"Motivos"}");
            Console.WriteLine("------------------------------------------------------------");
            foreach (var r in añadidas)
                Console.WriteLine($"{r.Nombre,-35} | {r.Provincia,-12} | {r.Municipio,-18} | {r.CodigoPostal,-6} | {string.Join("; ", r.Motivos)}");

            Console.WriteLine("\n ESTACIONES DESCARTADAS");
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine($"{"Nombre",-35} | {"Provincia",-12} | {"Municipio",-18} | {"CP",-6} | {"Motivos"}");
            Console.WriteLine("------------------------------------------------------------");
            foreach (var r in descartadas)
                Console.WriteLine($"{r.Nombre,-35} | {r.Provincia,-12} | {r.Municipio,-18} | {r.CodigoPostal,-6} | {string.Join("; ", r.Motivos)}");

            Console.WriteLine($"\n Total añadidas: {añadidas.Count}, descartadas: {descartadas.Count}");
        }

        public async Task<(List<ResultObject>, int, string, string)> LoadData()
        {
            try
            {
                var response = await _http.GetAsync("/cat/json");
                if (!response.IsSuccessStatusCode)
                {
                    return (new List<ResultObject>(), 0, "", "ERROR CAT HTTP");
                }

                var JsonCAT = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[CATExtractor] Recibido JSON de {JsonCAT.Length} caracteres");

                this.LoadFromString(JsonCAT);
                var datosParseados = this.ParseList();
                Console.WriteLine($"[CATExtractor] ParseList() devolvió {datosParseados.Count} objetos");

                var resultados = this.FromParsedToUsefull(datosParseados);

                return resultados; // Devuelve directamente la tupla
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepcion cargando datos de Cataluña {ex.Message}");
                return (new List<ResultObject>(), 0, "", "ERROR CAT");
            }
        }
    }
}
