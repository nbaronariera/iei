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
                    Nombre = dato.MUNICIPIO,
                    Provincia = dato.PROVINCIA,
                    Municipio = dato.MUNICIPIO,
                    CodigoPostal = dato.C_POSTAL,
                    Motivos = new List<string>()
                };


                // ---------------------------------------------------------
                // 1. CORRECCIÓN AUTOMÁTICA DE DATOS (Sanitización)
                // ---------------------------------------------------------

                // A. Si el municipio viene vacío (común en móviles), le asignamos "Itinerante"
                if (string.IsNullOrWhiteSpace(dato.MUNICIPIO) && dato.TIPO_ESTACION.Contains("Agrícola", StringComparison.OrdinalIgnoreCase))
                {
                    dato.MUNICIPIO = "Agrícola";
                }
                else if (string.IsNullOrWhiteSpace(dato.MUNICIPIO) && dato.TIPO_ESTACION.Contains("Móvil", StringComparison.OrdinalIgnoreCase))
                {
                    dato.MUNICIPIO = "Móvil";
                }
                else if(string.IsNullOrWhiteSpace(dato.MUNICIPIO))
                {
                    dato.MUNICIPIO = "Itinerante";
                }

                //Cambiamos nombre para el debug
                resultadoDebug.Nombre = dato.MUNICIPIO + " " + dato.Nº_ESTACION;

                resultadoDebug.Municipio = dato.MUNICIPIO;


                // B. Si la provincia viene vacía no lo incluimos
                if (string.IsNullOrWhiteSpace(dato.PROVINCIA))
                {
                    resultadoDebug.Motivos.Add("Provincia vacía."); 
                }

              


                // Normalizar variantes ortográficas comunes (València -> Valencia)
                if (!string.IsNullOrWhiteSpace(dato.PROVINCIA) &&
                    dato.PROVINCIA.Trim().Equals("València", StringComparison.OrdinalIgnoreCase))
                {
                    dato.PROVINCIA = "Valencia";
                }

                // Normalizar variantes ortográficas comunes (Alacant -> Alicante)
                if (!string.IsNullOrWhiteSpace(dato.PROVINCIA) &&
                    dato.PROVINCIA.Trim().Equals("Alacant", StringComparison.OrdinalIgnoreCase))
                {
                    dato.PROVINCIA = "Alicante";
                }

                // Normalizar variantes ortográficas comunes (Castelló -> Castellón)
                if (!string.IsNullOrWhiteSpace(dato.PROVINCIA) &&
                    dato.PROVINCIA.Trim().Equals("Castelló", StringComparison.OrdinalIgnoreCase))
                {
                    dato.PROVINCIA = "Castellón";
                }


                if(!string.IsNullOrWhiteSpace(dato.PROVINCIA) && !territoriosValidos.Contains(dato.PROVINCIA))
                {
                    resultadoDebug.Motivos.Add("Provincia no válida");
                }


                // C. LÓGICA DE CÓDIGO POSTAL INTELIGENTE
                string cpRaw = dato.C_POSTAL?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(cpRaw))
                {
                    // CASO 1: No tiene CP (Estaciones Móviles) -> Asignamos el genérico de la provincia
                    // Buscamos si la provincia está en tu diccionario (ej: Valencia -> 46)
                    var claveProvincia = prefijosCpPorTerritorio.Keys
                        .FirstOrDefault(k => k.Equals(dato.PROVINCIA, StringComparison.OrdinalIgnoreCase));

                    if (claveProvincia != null)
                    {
                        cpRaw = prefijosCpPorTerritorio[claveProvincia] + "000"; // Ej: "46" + "000" = "46000"
                    }
                    else
                    {
                        cpRaw = "00000"; // Fallback total si no encontramos la provincia
                    }
                }
                else 
                {
                    cpRaw = Regex.Replace(cpRaw, @"[^\d]", ""); // quitar cualquier carácter no numérico
                    if (cpRaw.Length < 5)
                        cpRaw = cpRaw.PadLeft(5, '0');
                }

                dato.C_POSTAL = cpRaw; // Guardamos el CP corregido para usarlo después
                resultadoDebug.CodigoPostal = dato.C_POSTAL;

                // ---------------------------------------------------------



                try
                {
                    // Validaciones (ahora es más difícil que fallen gracias a la corrección anterior)
                    if (string.IsNullOrWhiteSpace(dato.PROVINCIA))
                        resultadoDebug.Motivos.Add("Provincia vacía.");

                    if (string.IsNullOrWhiteSpace(dato.MUNICIPIO))
                        resultadoDebug.Motivos.Add("Municipio vacío.");

                    if (!Regex.IsMatch(dato.C_POSTAL, @"^\d{5}$"))
                    {
                        resultadoDebug.Motivos.Add($"Código postal inválido ('{dato.C_POSTAL}').");
                    }





                    // Gestión de Base de Datos (Provincias y Localidades)
                    var provincia = ObtenerOCrearProvincia(contexto, dato.PROVINCIA);
                    var localidad = ObtenerOCrearLocalidad(contexto, dato.MUNICIPIO, provincia);

                    if (provincia.nombre == "Desconocida" && dato.PROVINCIA != "Desconocida")
                    {
                        // Si quieres loguear algo aquí, puedes hacerlo
                    }

                    resultadoDebug.Provincia = provincia.nombre;

                    // Coordenadas y Tipo
                    double? lat = dato.Latitud, lon = dato.Longitud;

                    TipoEstacion tipo = TipoEstacion.Estacion_fija;
                    if (dato.TIPO_ESTACION != null)
                    {
                        if (dato.TIPO_ESTACION.Contains("Móvil", StringComparison.OrdinalIgnoreCase)) tipo = TipoEstacion.Estacion_movil;
                        else if (dato.TIPO_ESTACION.Contains("Agrícola", StringComparison.OrdinalIgnoreCase)) tipo = TipoEstacion.Otros;
                    }

                    // Chequeo de duplicados (Nº Estación + Coordenadas)
                    if (EstacionYaExiste(contexto, dato.Nº_ESTACION, lat ?? 0, lon ?? 0))
                    {
                        Debug.WriteLine($"[CV] Estación duplicada omitida: {dato.Nº_ESTACION}");
                        continue;
                    }

                    // Si hay errores graves, no insertamos
                    if (resultadoDebug.Motivos.Count > 0)
                    {
                        resultadoDebug.Añadida = false;
                        debugResultados.Add(resultadoDebug);
                        continue;
                    }

                    // Procesamiento de Horario
                    var horario = dato.HORARIOS ?? "Sin horario";
                    if (dato.TIPO_ESTACION != null && dato.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            horario = ConvertirFormatoFecha(dato.HORARIOS);
                        }
                        catch { /* Si falla el formato, dejamos el original */ }
                    }

                    // Creación del objeto Estacion
                    var estacion = new Estacion
                    {
                        nombre = string.IsNullOrWhiteSpace(dato.MUNICIPIO) ? (dato.DIRECCION ?? "Estación") : dato.MUNICIPIO + " " + dato.Nº_ESTACION,
                        tipo = tipo,
                        direccion = dato.DIRECCION ?? "Sin dirección",
                        codigoPostal = dato.C_POSTAL,
                        latitud = lat ?? 0,
                        longitud = lon ?? 0,
                        descripcion = dato.TIPO_ESTACION ?? "",
                        horario = horario,
                        contacto = $"Email: {dato.CORREOS}",
                        URL = "https://www.sitval.com/",
                        localidad = localidad,
                        codigoLocalidad = localidad.codigo
                    };

                    contexto.Estaciones.Add(estacion);
                    resultados.Add(new ResultObject { Estacion = estacion, Localidad = localidad, Provincia = provincia });
                    validas++;
                    noValidas--;

                    resultadoDebug.Añadida = true;
                    debugResultados.Add(resultadoDebug);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error insertando estación CV: {ex.Message}");
                    resultadoDebug.Motivos.Add($"Excepción: {ex.Message}");
                    debugResultados.Add(resultadoDebug);
                }
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