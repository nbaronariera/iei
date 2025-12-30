using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UI.Entidades;
using UI.Helpers;
using UI.Parsers.ParsedObjects;
using UI.Wrappers;

namespace UI.Parsers
{
    class ResultadoDebug
    {
        public string Fuente { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Provincia { get; set; } = "";
        public string Municipio { get; set; } = "";
        public string CodigoPostal { get; set; } = "";
        public bool Añadida { get; set; }
        public List<string> Motivos { get; set; } = new();
        public bool Reparada { get; set; }
        public List<string> Reparaciones { get; set; } = new();
    }

    public class GALExtractor : Parser<GALData>
    {

        private string Text { get; set; } = ""; // <-- esta línea corrige los errores

        HttpClient _http = new HttpClient { 
            BaseAddress = new Uri("http://localhost:8084"),
            Timeout = Timeout.InfiniteTimeSpan
        };
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
            string contenido = new StreamReader(file, Encoding.UTF8).ReadToEnd();
            Debug.WriteLine($"[GALExtractor] Cargando JSON de {contenido.Length} caracteres");

            var opciones = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // ← CLAVE: permite coincidencia sin importar mayúsculas
            };

            var filas = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(contenido, opciones) ?? new List<Dictionary<string, string>>();

            var resultado = new List<GALData>();
            foreach (var fila in filas)
            {
                var gal = new GALData
                {
                    // Usa las claves originales del CSV convertido
                    NombreEstacion = Obtener(fila, "NOME DA ESTACIÓN"),
                    Direccion = Obtener(fila, "ENDEREZO"),
                    Municipio = Obtener(fila, "CONCELLO"),
                    CodigoPostal = Obtener(fila, "CÓDIGO POSTAL"),
                    Provincia = Obtener(fila, "PROVINCIA"),
                    Telefono = Obtener(fila, "TELÉFONO"),
                    HorarioRaw = Obtener(fila, "HORARIO"),
                    UrlCita = Obtener(fila, "SOLICITUDE DE CITA PREVIA"),
                    Correo = Obtener(fila, "CORREO ELECTRÓNICO"),
                    Coordenadas = Obtener(fila, "COORDENADAS GMAPS")
                };
                resultado.Add(gal);
            }

            Debug.WriteLine($"[GALExtractor] Convertidos a {resultado.Count} objetos GALData");
            return resultado;
        }

        private string Obtener(Dictionary<string, string> fila, string clave)
        {
            return fila.TryGetValue(clave, out var valor) ? valor?.Trim() ?? "" : "";
        }

        public (List<ResultObject>, int, String, String) FromParsedToUsefull(List<GALData> datosParseados)
        {
            var resultados = new List<ResultObject>();
            var estacionesValidas = new List<Estacion>();
            using var contexto = new AppDbContext();
            var debugResultados = new List<ResultadoDebug>();

            var nombresEnEsteFichero = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var coordenadasEnEsteFichero = new HashSet<string>();

            Console.WriteLine($"Iniciando parseo de {datosParseados.Count} registros GAL.");

            foreach (var dato in datosParseados)
            {
                var resultadoDebug = new ResultadoDebug
                {
                    Nombre = dato?.NombreEstacion ?? "(sin nombre)",
                    Provincia = dato?.Provincia ?? "",
                    Municipio = dato?.Municipio ?? "",
                    CodigoPostal = dato?.CodigoPostal ?? "",
                    Motivos = new List<string>(),
                    Reparaciones = new List<string>(),
                    Añadida = true,
                    Reparada = false
                };
                resultadoDebug.Fuente = "GAL";

                // Normalización de provincia
                string provinciaOriginal = dato?.Provincia?.Trim() ?? "";

                if (!string.IsNullOrWhiteSpace(dato.Provincia) &&
                    dato.Provincia.Trim().Equals("La Coruña", StringComparison.OrdinalIgnoreCase))
                {
                    dato.Provincia = "A Coruña";
                    resultadoDebug.Reparada = true;
                }
                if (!string.IsNullOrWhiteSpace(dato.Provincia) &&
                    dato.Provincia.Trim().Equals("Orense", StringComparison.OrdinalIgnoreCase))
                {
                    dato.Provincia = "Ourense";
                    resultadoDebug.Reparada = true;
                }

                if (string.IsNullOrWhiteSpace(dato.NombreEstacion))
                {
                    resultadoDebug.Motivos.Add("Nombre de estación vacío o nulo.");
                    resultadoDebug.Añadida = false;
                }
                if (string.IsNullOrWhiteSpace(dato.Provincia))
                {
                    resultadoDebug.Motivos.Add("Provincia vacía o nula.");
                    resultadoDebug.Añadida = false;
                }
                if (string.IsNullOrWhiteSpace(dato.Municipio))
                {
                    resultadoDebug.Motivos.Add("Municipio vacío o nulo.");
                    resultadoDebug.Añadida = false;
                }
                if (!int.TryParse(dato.CodigoPostal, out int codigoPostal) || codigoPostal < 10000 || codigoPostal > 99999)
                {
                    resultadoDebug.Motivos.Add($"Código postal inválido ('{dato.CodigoPostal}'), al no tener 5 caracteres");
                    resultadoDebug.Añadida = false;
                }
                if (dato.CodigoPostal.Length >= 2)
                {
                    var cpPrefijo = dato.CodigoPostal.Substring(0, 2);
                    var prefijosValidos = provinciasGallegas.Values.Select(v => v.ToString("D2")).ToHashSet();
                    if (!prefijosValidos.Contains(cpPrefijo))
                    {
                        resultadoDebug.Motivos.Add("El prefijo del código postal no coincide con ninguna provincia gallega");
                        resultadoDebug.Añadida = false;
                    }
                }
                if (!provincias.Contains(dato.Provincia.Trim()))
                {
                    resultadoDebug.Motivos.Add("Provincia no válida.");
                    resultadoDebug.Añadida = false;
                }
                else if (!CodigoPostalValido(codigoPostal, dato.Provincia))
                {
                    resultadoDebug.Motivos.Add($"Código postal {codigoPostal} no coincide con provincia '{dato.Provincia}'.");
                    resultadoDebug.Añadida = false;
                }

                double lat = ExtraerLatitud(dato.Coordenadas);
                double lon = ExtraerLongitud(dato.Coordenadas);
                string coordsKey = $"{lat:F6},{lon:F6}";

                if (!EsCoordenadaEnEspañaPeninsular(lat, lon))
                {
                    resultadoDebug.Motivos.Add($"Coordenadas fuera de España peninsular ({lat}, {lon}).");
                    resultadoDebug.Añadida = false;
                }

                string nombreNormalizado = dato.NombreEstacion?.Trim() ?? "";

                // Duplicados internos
                if (!string.IsNullOrWhiteSpace(nombreNormalizado) && !nombresEnEsteFichero.Add(nombreNormalizado))
                {
                    resultadoDebug.Motivos.Add($"Nombre repetido dentro del mismo archivo GAL ({nombreNormalizado}).");
                    resultadoDebug.Añadida = false;
                }
                if (lat != 0 && lon != 0 && !coordenadasEnEsteFichero.Add(coordsKey))
                {
                    resultadoDebug.Motivos.Add($"Coordenadas repetidas dentro del mismo archivo GAL ({lat:F6}, {lon:F6}).");
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

                if (!resultadoDebug.Añadida)
                {
                    debugResultados.Add(resultadoDebug);
                    continue;
                }

                // === Solo si se añade, registramos el mensaje de reparación ===
                if (resultadoDebug.Reparada)
                {
                    if (provinciaOriginal.Equals("La Coruña", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Reparaciones.Add("Para arreglar la provincia, esta se normalizó, pasándola de La Coruña a A Coruña.");
                        resultadoDebug.Motivos.Add("Provincia normalizada: de La Coruña a A Coruña.");
                    }
                    else if (provinciaOriginal.Equals("Orense", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Reparaciones.Add("Para arreglar la provincia, esta se normalizó, pasándola de Orense a Ourense.");
                        resultadoDebug.Motivos.Add("Provincia normalizada: de Orense a Ourense.");
                    }
                }

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

                estacionesValidas.Add(estacion);
                resultados.Add(new ResultObject
                {
                    Estacion = estacion,
                    Localidad = localidad,
                    Provincia = provincia
                });
                debugResultados.Add(resultadoDebug);
            }

            if (estacionesValidas.Any())
            {
                contexto.Estaciones.AddRange(estacionesValidas);
                contexto.SaveChanges();
            }

            MostrarResumen(debugResultados);
            int agregadas = resultados.Count;
            string estacionesReparadas = string.Join("\n",
                debugResultados.Where(r => r.Reparada && r.Añadida)
                    .Select(r => $"{{{r.Fuente}, {r.Nombre}, {r.Municipio}, [{string.Join("; ", r.Motivos)}], [{string.Join("; ", r.Reparaciones)}]}}"));

            string estacionesRechazadas = string.Join("\n",
                debugResultados.Where(r => !r.Añadida)
                    .Select(r => $"{{{r.Fuente}, {r.Nombre}, {r.Municipio}, [{string.Join("; ", r.Motivos)}]}}"));

            return (resultados, agregadas, estacionesReparadas, estacionesRechazadas);
        }

        private Provincia ObtenerOCrearProvincia(AppDbContext ctx, string nombre)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));

            if (string.IsNullOrWhiteSpace(nombre))
                nombre = "Desconocida";

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
            if (string.IsNullOrWhiteSpace(nombre)) nombre = "Desconocida";

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
              //  .Replace("", "°")
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

            // Convertimos el código postal a string para extraer los dos primeros dígitos
            string cpStr = codigo.ToString();

            if (cpStr.Length < 2) return false; // Si tiene menos de 2 dígitos, inválido

            string prefijoCP = cpStr.Substring(0, 2); // Siempre los 2 primeros: "27" para 271003, 27001, 27, etc.

            // Obtenemos el prefijo esperado para la provincia (como string "27" para Lugo)
            if (!provinciasGallegas.TryGetValue(provincia.Trim(), out int prefijoEsperado))
                return false;

            string prefijoEsperadoStr = prefijoEsperado.ToString("D2"); // Asegura 2 dígitos: 15 → "15", 27 → "27"

            return prefijoCP == prefijoEsperadoStr;
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

            Console.WriteLine("\n ESTACIONES AÑADIDAS");
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine($"{"Nombre",-35} | {"Provincia",-12} | {"CP",-6} | {"Motivos"}");
            Console.WriteLine("------------------------------------------------------------");
            foreach (var r in añadidas)
                Console.WriteLine($"{r.Nombre,-35} | {r.Provincia,-12} | {r.CodigoPostal,-6} | {string.Join("; ", r.Motivos)}");

            Console.WriteLine("\n ESTACIONES DESCARTADAS");
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine($"{"Nombre",-35} | {"Provincia",-12} | {"CP",-6} | {"Motivos"}");
            Console.WriteLine("------------------------------------------------------------");
            foreach (var r in descartadas)
                Console.WriteLine($"{r.Nombre,-35} | {r.Provincia,-12} | {r.CodigoPostal,-6} | {string.Join("; ", r.Motivos)}");

            Console.WriteLine($"\n Total añadidas: {añadidas.Count}, descartadas: {descartadas.Count}");
        }

        public async Task<(List<ResultObject>, int, string, string)> LoadData()
        {
            try
            {
                var response = await _http.GetAsync("/gal/json");
                if (!response.IsSuccessStatusCode)
                {
                    return (new List<ResultObject>(), 0, "", "ERROR GAL HTTP");
                }

                var JsonGAL = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GALExtractor] Recibido JSON de {JsonGAL.Length} caracteres");

                this.LoadFromString(JsonGAL);
                var datosParseados = this.ParseList();
                Console.WriteLine($"[GALExtractor] ParseList() devolvió {datosParseados.Count} objetos");

                return this.FromParsedToUsefull(datosParseados);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepcion cargando datos de Galicia {ex.Message} ");
                return (new List<ResultObject>(), 0, "", "ERROR GAL");
            }
        }





    }
}