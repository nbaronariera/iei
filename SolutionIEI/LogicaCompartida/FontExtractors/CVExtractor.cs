using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UI.Entidades;
using UI.Helpers;
using UI.Parsers.ParsedObjects;
using UI.Wrappers;
using static System.Net.WebRequestMethods;

namespace UI.Parsers
{
    public class CVExtractor : Parser<JSONData>
    {

        static CoordenadasSelenium seleniumHelper = new CoordenadasSelenium();
        HttpClient _http = new HttpClient { 
            BaseAddress = new Uri("http://localhost:8082"),
            Timeout = Timeout.InfiniteTimeSpan
        };

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

        public (List<ResultObject>, int, String, String) FromParsedToUsefull(List<JSONData> datosParseados)
        {

            Debug.WriteLine($"[CVExtractor] Procesando {datosParseados.Count} registros...");
            var resultados = new List<ResultObject>();
            using var contexto = new AppDbContext();
            var debugResultados = new List<ResultadoDebug>();
            
            int numValidas = 0;

            foreach (var dato in datosParseados)
            {

               

                var resultadoDebug = new ResultadoDebug
                {
                    Nombre = dato.MUNICIPIO,
                    Provincia = dato.PROVINCIA,
                    Municipio = dato.MUNICIPIO,
                    CodigoPostal = dato.C_POSTAL,
                    Motivos = new List<string>(),
                    Añadida = true  // ← ¡Empieza como true!
                };

                resultadoDebug.Fuente = "CV";

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
                else if (string.IsNullOrWhiteSpace(dato.MUNICIPIO))
                {
                    dato.MUNICIPIO = "Itinerante";
                }

                //Cambiamos nombre para el debug
                resultadoDebug.Nombre = dato.MUNICIPIO + " " + dato.Nº_ESTACION;

                resultadoDebug.Municipio = dato.MUNICIPIO;

                // Normalizar variantes ortográficas comunes (València -> Valencia)
                if (!string.IsNullOrWhiteSpace(dato.PROVINCIA) &&
                    dato.PROVINCIA.Trim().Equals("València", StringComparison.OrdinalIgnoreCase))
                {
                    dato.PROVINCIA = "Valencia";
                    resultadoDebug.Reparada = true;
                    resultadoDebug.Motivos.Add("Provincia incorrecta: València");
                    resultadoDebug.Reparaciones.Add("Provincia normalizada: València → Valencia");
                }

                // Normalizar variantes ortográficas comunes (Alacant -> Alicante)
                if (!string.IsNullOrWhiteSpace(dato.PROVINCIA) &&
                    dato.PROVINCIA.Trim().Equals("Alacant", StringComparison.OrdinalIgnoreCase))
                {
                    dato.PROVINCIA = "Alicante";
                    resultadoDebug.Reparada = true;
                    resultadoDebug.Motivos.Add("Provincia incorrecta: Alacant");
                    resultadoDebug.Reparaciones.Add("Provincia normalizada: Alacant → Alicante");
                }

                // Normalizar variantes ortográficas comunes (Castelló -> Castellón)
                if (!string.IsNullOrWhiteSpace(dato.PROVINCIA) &&
                    dato.PROVINCIA.Trim().Equals("Castelló", StringComparison.OrdinalIgnoreCase))
                {
                    dato.PROVINCIA = "Castellón";
                    resultadoDebug.Reparada = true;
                    resultadoDebug.Motivos.Add("Provincia incorrecta: Castelló");
                    resultadoDebug.Reparaciones.Add("Provincia normalizada: Castelló → Castellón");
                }




                // C. LÓGICA DE CÓDIGO POSTAL INTELIGENTE
                string cpRaw = dato.C_POSTAL?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(cpRaw) || dato.TIPO_ESTACION.Contains("Agrícola", StringComparison.OrdinalIgnoreCase) ||
                    dato.TIPO_ESTACION.Contains("Móvil", StringComparison.OrdinalIgnoreCase))
                {
                    // CASO 1: No tiene CP  o es estacion movil u agricola -> Asignamos el genérico de la provincia
                    // Buscamos si la provincia está en tu diccionario (ej: Valencia -> 46)
                    var claveProvincia = prefijosCpPorTerritorio.Keys
                        .FirstOrDefault(k => k.Equals(dato.PROVINCIA, StringComparison.OrdinalIgnoreCase));

                    dato.DIRECCION = "";

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

                try
                {
                    // Validaciones (ahora es más difícil que fallen gracias a la corrección anterior)
                    if (string.IsNullOrWhiteSpace(dato.PROVINCIA))
                    {
                        resultadoDebug.Motivos.Add("Provincia vacía.");
                        resultadoDebug.Añadida = false;
                    }

                    if (string.IsNullOrWhiteSpace(dato.MUNICIPIO))
                    {
                        resultadoDebug.Motivos.Add("Municipio vacío.");
                        resultadoDebug.Añadida = false;
                    }

                    if (!Regex.IsMatch(dato.C_POSTAL, @"^\d{5}$"))
                    {
                        resultadoDebug.Motivos.Add($"Código postal inválido ('{dato.C_POSTAL}'), al no tener 5 caracteres");
                        resultadoDebug.Añadida = false;
                    }


                    if (!string.IsNullOrWhiteSpace(dato.PROVINCIA) && !territoriosValidos.Contains(dato.PROVINCIA))
                    {
                        resultadoDebug.Motivos.Add("Provincia no válida");
                        resultadoDebug.Añadida = false;
                    }
                    else if (!string.IsNullOrWhiteSpace(dato.PROVINCIA) && !CodigoPostalValido(dato.C_POSTAL, dato.PROVINCIA))
                    {
                        resultadoDebug.Motivos.Add($"Código postal {dato.C_POSTAL} no coincide con provincia '{dato.PROVINCIA}'.");
                        resultadoDebug.Añadida = false;
                    }

                    if (dato.C_POSTAL.Length >= 2)
                    {
                        var cpPrefijo = dato.C_POSTAL.Substring(0, 2);
                        var prefijosValidos = prefijosCpPorTerritorio.Values.ToHashSet();

                        if (!prefijosValidos.Contains(cpPrefijo))
                        {
                            resultadoDebug.Motivos.Add("El prefijo del código postal no coincide con Castellón, Valencia o Alicante");
                        }
                    }

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
                        resultadoDebug.Motivos.Add("Estación duplicada.");
                        resultadoDebug.Añadida = false;

                    }

                    // Si hay errores graves, no insertamos
                    if (resultadoDebug.Añadida == false)
                    {
                        resultadoDebug.Añadida = false;
                        debugResultados.Add(resultadoDebug);
                        continue;
                    }

                    if (lat == 0 && lon == 0 && tipo == TipoEstacion.Estacion_fija)
                    {
                        resultadoDebug.Añadida = false;
                        resultadoDebug.Motivos.Add("No se pudieron obtener las coordenadas (Selenium falló o no encontró)");
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

                    // Gestión de Base de Datos (Provincias y Localidades)
                    var provincia = ObtenerOCrearProvincia(contexto, dato.PROVINCIA);
                    var localidad = ObtenerOCrearLocalidad(contexto, dato.MUNICIPIO, provincia);



                    resultadoDebug.Provincia = provincia.nombre;

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
                        contacto = $"Correo electrónico: {dato.CORREOS}",
                        URL = "https://www.sitval.com/",
                        localidad = localidad,
                        codigoLocalidad = localidad.codigo
                    };

                    contexto.Estaciones.Add(estacion);
                    resultados.Add(new ResultObject { Estacion = estacion, Localidad = localidad, Provincia = provincia });
                    numValidas++;

                    Debug.WriteLine($"[CVExtractor] Carga completada: {resultados.Count} añadidas, {debugResultados.Count(r => !r.Añadida)} rechazadas");
                    resultadoDebug.Añadida = true;
                    debugResultados.Add(resultadoDebug);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error insertando estación CV: {ex.Message}");
                    resultadoDebug.Motivos.Add($"Excepción: {ex.Message}");
                    debugResultados.Add(resultadoDebug);
                }
            }

            contexto.SaveChanges();
            MostrarResumen(debugResultados);

            int omitidas = debugResultados.Count(r => !r.Añadida);

            string estacionesReparadas = string.Join("\n",
                debugResultados
                    .Where(r => r.Reparada && r.Añadida)
                    .Select(r =>
                        $"{{{r.Fuente}, {r.Nombre}, {r.Municipio}, [{string.Join("; ", r.Motivos)}], [{string.Join("; ", r.Reparaciones)}]}}"
                    )
            );

            string estacionesRechazadas = string.Join("\n",
                debugResultados
                    .Where(r => !r.Añadida)
                    .Select(r =>
                        $"{{{r.Fuente}, {r.Nombre}, {r.Municipio}, [{string.Join("; ", r.Motivos)}]}}"
                    )
            );

            return (resultados, omitidas, estacionesReparadas, estacionesRechazadas);

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

        private bool CodigoPostalValido(string codigoPostal, string provincia)
        {
            if (string.IsNullOrWhiteSpace(codigoPostal) || codigoPostal.Length < 2)
                return false;

            if (!prefijosCpPorTerritorio.TryGetValue(provincia.Trim(), out string prefijo))
                return false;

            // Compara los dos primeros dígitos del CP con el prefijo de la provincia
            return codigoPostal.StartsWith(prefijo);
        }

        private string ConvertirFormatoFecha(string input)
        {
            return input;
        }

        public async Task<(List<ResultObject>, int, string, string)> LoadData()
        {
            Debug.WriteLine("[CVExtractor] === INICIO CARGA COMUNIDAD VALENCIANA ===");
            try
            {
                var response = await _http.GetAsync("/cv/json");
                Debug.WriteLine($"[CVExtractor] Respuesta HTTP: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    return (new List<ResultObject>(), 0, "", "ERROR CV HTTP");
                }

               

                var JsonCV = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[CVExtractor] Recibido JSON de {JsonCV.Length} caracteres");

                this.LoadFromString(JsonCV);
                var datosParseados = this.ParseList();
                Debug.WriteLine($"[CVExtractor] ParseList() devolvió {datosParseados.Count} objetos");

                var resultados = this.FromParsedToUsefull(datosParseados);
                Debug.WriteLine($"[CVExtractor] === FIN CARGA: {resultados.Item1.Count} estaciones añadidas ===");
                return resultados; // Devuelve directamente la tupla
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepcion cargando datos de CV {ex.Message}");
                return (new List<ResultObject>(), 0, "", "ERROR CV");
            }
        }

        public override void LoadFromString(string json)
        {

            Debug.WriteLine("[CVExtractor] LoadFromString iniciado");

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var listaObjetos = JsonSerializer.Deserialize<List<JSONData>>(json, options);

                if (listaObjetos == null || listaObjetos.Count == 0)
                {
                    Console.WriteLine("[CVExtractor] JSON vacío o inválido → no se procesa nada");
                    return;
                }

                Console.WriteLine($"[CVExtractor] Parseados {listaObjetos.Count} objetos del JSON original");

                // Aplicamos Selenium directamente sobre los objetos en memoria
                ApplySelenium(listaObjetos);

                // Guardamos los objetos modificados en la propiedad 'file' del parser base
                // para que ExecuteParse() los lea (aunque no usemos ExecuteParse, el base lo necesita)
                string tempJson = JsonSerializer.Serialize(listaObjetos, new JsonSerializerOptions { WriteIndented = false });
                var tempStream = new MemoryStream(Encoding.UTF8.GetBytes(tempJson));
                this.file = tempStream;  // 'file' es la propiedad protegida del Parser<T> base

                Debug.WriteLine("[CVExtractor] Objetos modificados con coordenadas y listos para procesar");
            }
            catch (Exception ex)
            {
              var sb = new StringBuilder();
                sb.AppendLine($"[CRITICAL ERROR] Fallo en LoadFromString.");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"StackTrace: {ex.StackTrace}");

                // Profundizar en el error real
                var inner = ex.InnerException;
                while (inner != null)
                {
                    sb.AppendLine("--- Inner Exception ---");
                    sb.AppendLine($"Message: {inner.Message}");
                    sb.AppendLine($"StackTrace: {inner.StackTrace}");
                    inner = inner.InnerException;
                }

                Console.WriteLine(sb.ToString());
            }
        }

        private static void ApplySelenium(List<JSONData> elementos)
        {
            Debug.WriteLine($"[CVExtractor] Aplicando Selenium a {elementos.Count} elementos");

            foreach (var elemento in elementos)
            {
                bool esEstacionFija = elemento.TIPO_ESTACION != null &&
                                      elemento.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase);

                if (esEstacionFija && (elemento.Latitud == null || elemento.Latitud == 0 || elemento.Longitud == null || elemento.Longitud == 0))
                {
                    Debug.WriteLine($"[SELENIUM] Obteniendo coordenadas para: {elemento.DIRECCION ?? "sin dirección"}, {elemento.MUNICIPIO ?? "sin municipio"}");

                    try
                    {
                        var coords = seleniumHelper.ObtenerCoordenadas(elemento.DIRECCION ?? "", elemento.MUNICIPIO ?? "");
                        elemento.Latitud = coords.Lat;
                        elemento.Longitud = coords.Lng;

                        if (coords.Lat != 0 || coords.Lng != 0)
                        {
                            Debug.WriteLine($"[SELENIUM] ÉXITO → ({coords.Lat}, {coords.Lng})");
                        }
                        else
                        {
                            Debug.WriteLine("[SELENIUM] No encontradas → usando (0,0)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SELENIUM] Error en esta estación: {ex.Message}");
                        elemento.Latitud = 0.0;
                        elemento.Longitud = 0.0;
                    }
                }
                else
                {
                    // Móviles o agrícolas → coordenadas 0
                    elemento.Latitud ??= 0.0;
                    elemento.Longitud ??= 0.0;
                }
            }

            Debug.WriteLine("[CVExtractor] Selenium aplicado a todos los elementos");
        }
    }
}