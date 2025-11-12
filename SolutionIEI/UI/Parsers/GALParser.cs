using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UI.Entidades;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
        public class GALParser : Parser<GALData>
        {
            // Diccionario de correcciones específicas
            private static readonly Dictionary<string, string> Correcciones = new()
        {
            // Provincia
            { "A Coru�a", "A Coruña" },
            // Localidades
            { "Nar�n", "Narón" },
            { "Carballi�o, O", "Carballiño, O" },
            { "Ver�n", "Verín" },
            { "Villagarc�a de Arousa", "Villagarcía de Arousa" },
            { "Porri�o, O", "Porriño, O" },
            { "Lal�n", "Lalín" },
            { "Nigr�n", "Nigrán" }
        };

            protected override List<GALData> ExecuteParse()
            {
                if (file == null) return new List<GALData>();

                var opciones = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                string contenido = new StreamReader(file, Encoding.UTF8).ReadToEnd();
                return JsonSerializer.Deserialize<List<GALData>>(contenido, opciones) ?? new List<GALData>();
            }

            protected List<ResultObject> FromParsedToUsefull(List<GALData> datosParseados)
            {
                var resultados = new List<ResultObject>();
                using var contexto = new AppDbContext();

                foreach (var dato in datosParseados)
                {
                    // 1 Eliminar algunos de los caracteres invalidos en nombres de estación, provincia y localidad
                    dato.NombreEstacion = CorregirNombre(dato.NombreEstacion, esEstacion: true);
                    dato.Provincia = CorregirNombre(dato.Provincia, esProvincia: true);
                    dato.Municipio = CorregirNombre(dato.Municipio, esLocalidad: true);

                    // 2 Validar que Provincia y Municipio no estén vacíos/sean null 
                    // o que el código postal sea un entero positivo
                    if (string.IsNullOrWhiteSpace(dato.Provincia) ||
                        string.IsNullOrWhiteSpace(dato.Municipio) ||
                        !int.TryParse(dato.CodigoPostal, out int cp) || cp <= 0)
                    {
                        continue; // Estación inválida, se omite su inclusión
                    }

                    // 3 Obtener coordenadas 
                    double lat = ExtraerLatitud(dato.Coordenadas);
                    double lon = ExtraerLongitud(dato.Coordenadas);

                    if (!EsCoordenadaEnEspañaPeninsular(lat, lon))
                    {
                        continue; // Fuera de la península, por lo tanto no se incluye la estación
                    }

                    // 4. Comprobar si existen provincias o localidades para no repetirlas en la BD
                    var provincia = ObtenerOCrearProvincia(contexto, dato.Provincia);
                    var localidad = ObtenerOCrearLocalidad(contexto, dato.Municipio, provincia);

                    var estacion = new Estacion
                    {
                        nombre = dato.NombreEstacion,
                        tipo = TipoEstacion.Estacion_fija,
                        direccion = dato.Direccion,
                        codigoPostal = cp,
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
                }

                contexto.SaveChanges(); // Guardar todas las estaciones en la BD
                return resultados;
            }

            // Métodos auxiliares

            private string CorregirNombre(string original, bool esEstacion = false, bool esProvincia = false, bool esLocalidad = false)
            {
                if (string.IsNullOrWhiteSpace(original)) return original;

                if (esEstacion)
                {
                    original = Regex.Replace(original, @"^Estaci�n", "Estación", RegexOptions.IgnoreCase);
                }

                if (Correcciones.TryGetValue(original.Trim(), out string corregido))
                {
                    return corregido;
                }

                return original;
            }

            private Provincia ObtenerOCrearProvincia(AppDbContext ctx, string nombre)
            {
                var existente = ctx.Provincias.FirstOrDefault(p => p.nombre == nombre);
                if (existente != null) return existente;

                var nueva = new Provincia(nombre);
                ctx.Provincias.Add(nueva);
                ctx.SaveChanges();
                return nueva;
            }

            private Localidad ObtenerOCrearLocalidad(AppDbContext ctx, string nombre, Provincia provincia)
            {
                var existente = ctx.Localidades
                    .FirstOrDefault(l => l.nombre == nombre && l.codigoProvincia == provincia.codigo);

                if (existente != null) return existente;

                var nueva = new Localidad(nombre)
                {
                    Provincia = provincia,
                    codigoProvincia = provincia.codigo
                };
                ctx.Localidades.Add(nueva);
                ctx.SaveChanges();
                return nueva;
            }

            private double ExtraerLatitud(string coordenadas) =>
                ParsearCoordenadaDMS(coordenadas?.Split(',')[0]?.Trim() ?? "");

            private double ExtraerLongitud(string coordenadas) =>
                ParsearCoordenadaDMS(coordenadas?.Split(',')[1]?.Trim() ?? "");

            private double ParsearCoordenadaDMS(string dms)
            {
                if (string.IsNullOrWhiteSpace(dms)) return 0;

                // 1 Decimal directo: "42.906076", utilizable directamente por la interfaz
                if (double.TryParse(dms, NumberStyles.Any, CultureInfo.InvariantCulture, out double valorDirecto))
                {
                    return valorDirecto;
                }

                // 2 DMS: "43° 2.576'" o "43 2.576", se ha de hacer la conversión para la interfaz
                dms = dms.Replace("�", "°").Replace("°", " ").Replace("'", "").Replace("\"", "").Trim();
                var partes = Regex.Split(dms, @"\s+|\.");

                double grados = 0, minutos = 0;
                if (partes.Length >= 1) double.TryParse(partes[0], out grados);
                if (partes.Length >= 2)
                {
                    string minutosStr = string.Join(".", partes.Skip(1));
                    double.TryParse(minutosStr, out minutos);
                }

                return grados + (minutos / 60.0);
            }

            private bool EsCoordenadaEnEspañaPeninsular(double lat, double lon)
            {
                const double latMin = 36.0, latMax = 43.8;
                const double lonMin = -9.3, lonMax = 3.3;

                return lat >= latMin && lat <= latMax && lon >= lonMin && lon <= lonMax;
            }

            private string ConvertirHorario(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return "";

                raw = raw.ToLower()
                         .Replace(" horas", "")
                         .Replace(".", ":");

                string dias = "";
                string resto = raw;

                // Detectar días
                if (raw.Contains("luns a venres"))
                {
                    dias = "L-V";
                    resto = Regex.Replace(resto, @" ?\(?de luns a venres\)?", "", RegexOptions.IgnoreCase);
                }
                else if (raw.Contains("sábados"))
                {
                    dias = "S";
                    resto = Regex.Replace(resto, @" ?\(?sábados?\)?", "", RegexOptions.IgnoreCase);
                }

                // Extraer rangos: "de 8:00 a 14:00 e de 16:00 a 19:00"
                var rangos = new List<string>();
                var matches = Regex.Matches(resto, @"de (\d+:\d+) a (\d+:\d+)", RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    rangos.Add($"{m.Groups[1].Value}-{m.Groups[2].Value}");
                }

                string horas = string.Join(",", rangos);
                return string.IsNullOrEmpty(dias) ? horas : $"{dias} ({horas})";
            }

            private string FormatearContacto(string correo, string telefono)
            {
                return $"Correo electrónico: {correo} Teléfono: {telefono}";
            }
        }
}

