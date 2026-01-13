using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
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
    // Clase encargada de interpretar y transformar el JSON específico de la Comunidad Valenciana.
    // Realiza la extracción de datos, la limpieza (sanitización), validación y carga en la base de datos.
    public class CVExtractor : Parser<JSONData>
    {
        private List<JSONData> objetosParseados = new List<JSONData>();

        // Cliente HTTP para conectar con la API Wrapper de la Comunidad Valenciana (Puerto 8082)
        HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:8082"),
            Timeout = Timeout.InfiniteTimeSpan
        };

        // Lista blanca de territorios válidos para filtrar datos erróneos de origen.
        // Se utiliza para validación cruzada entre el código postal y la provincia declarada.
        private static readonly HashSet<string> territoriosValidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "Castellón", "Valencia", "Alicante"
        };

        // Diccionario para inferir códigos postales cuando faltan, basándonos en la provincia.
        // Es esencial para las estaciones móviles/agrícolas que no tienen una dirección física fija
        // y por tanto carecen de CP en el JSON original.
        private static readonly Dictionary<string, string> prefijosCpPorTerritorio = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Castellón", "12" },
            { "Valencia", "46" },
            { "Alicante", "03" },
        };

        // Método principal de deserialización del archivo fuente local (si se usara archivo directo).
        protected override List<JSONData> ExecuteParse()
        {
            if (file == null) return new List<JSONData>();
            string contenido = new StreamReader(file, Encoding.UTF8).ReadToEnd();
            // CaseInsensitive es vital porque las claves del JSON a veces cambian mayúsculas/minúsculas 
            // en los datos de origen (ej: "Municipio" vs "MUNICIPIO").
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<JSONData>>(contenido, opciones) ?? new List<JSONData>();
        }

        // Método principal de Transformación (La 'T' de ETL).
        // Convierte objetos JSON crudos en entidades de negocio 'Estacion' validadas.
        public (List<ResultObject>, int, String, String) FromParsedToUsefull(List<JSONData> datosParseados)
        {
            Debug.WriteLine($"[CVExtractor] Procesando {datosParseados.Count} registros...");
            var resultados = new List<ResultObject>();
            var estacionesValidas = new List<Estacion>();
            using var contexto = new AppDbContext();
            var debugResultados = new List<ResultadoDebug>();

            // Sets para detección de duplicados DENTRO del mismo fichero antes de ir a BD
            var nombresEnEsteFichero = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var coordenadasEnEsteFichero = new HashSet<string>();

            foreach (var dato in datosParseados)
            {
                var resultadoDebug = new ResultadoDebug
                {
                    Nombre = dato.MUNICIPIO,
                    Provincia = dato.PROVINCIA,
                    Municipio = dato.MUNICIPIO,
                    CodigoPostal = dato.C_POSTAL,
                    Motivos = new List<string>(),
                    Reparaciones = new List<string>(),
                    Añadida = true,
                    Reparada = false
                };
                resultadoDebug.Fuente = "CV";

                resultadoDebug.Municipio = dato.MUNICIPIO;

                // 1. CORRECCIÓN AUTOMÁTICA DE DATOS (SANITIZACIÓN)
                // Normalización lingüística.
                // Unificamos nombres en valenciano/catalán a su versión en castellano para mantener
                // consistencia en las búsquedas (ej: València -> Valencia).
                bool provinciaReparada = false;
                string provinciaOriginal = dato.PROVINCIA?.Trim() ?? "";

                if (!string.IsNullOrWhiteSpace(dato.PROVINCIA))
                {
                    if (dato.PROVINCIA.Trim().Equals("València", StringComparison.OrdinalIgnoreCase))
                    {
                        dato.PROVINCIA = "Valencia";
                        provinciaReparada = true;
                    }
                    else if (dato.PROVINCIA.Trim().Equals("Alacant", StringComparison.OrdinalIgnoreCase))
                    {
                        dato.PROVINCIA = "Alicante";
                        provinciaReparada = true;
                    }
                    else if (dato.PROVINCIA.Trim().Equals("Castelló", StringComparison.OrdinalIgnoreCase))
                    {
                        dato.PROVINCIA = "Castellón";
                        provinciaReparada = true;
                    }
                }

                resultadoDebug.CodigoPostal = dato.C_POSTAL;

                // 2. VALIDACIÓN Y PERSISTENCIA
                try
                {
                    // Validaciones de integridad obligatorias
                    if (string.IsNullOrWhiteSpace(dato.PROVINCIA))
                    {
                        resultadoDebug.Motivos.Add("Provincia vacía.");
                        resultadoDebug.Añadida = false;
                    }
                    // Regla de negocio: Estaciones fijas deben tener municipio.
                    if (string.IsNullOrWhiteSpace(dato.MUNICIPIO) && dato.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Motivos.Add("Municipio vacío.");
                        resultadoDebug.Añadida = false;
                    }
                    // Regla de negocio: Estaciones móviles/agrícolas NO deben tener municipio (son itinerantes).
                    if (!string.IsNullOrWhiteSpace(dato.MUNICIPIO) && !dato.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Motivos.Add("El municipio de una estación no fija ha de estar vacío.");
                        resultadoDebug.Añadida = false;
                    }
                    // Validación de formato de Código Postal (5 dígitos) para fijas.
                    if (!Regex.IsMatch(dato.C_POSTAL, @"^\d{5}$") && dato.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Motivos.Add($"Código postal inválido ('{dato.C_POSTAL}'), al no tener 5 caracteres.");
                        resultadoDebug.Añadida = false;
                    }
                    // Regla: Estaciones no fijas no deben tener CP específico.
                    if (!string.IsNullOrWhiteSpace(dato.C_POSTAL) && !dato.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Motivos.Add($"El código postal de una estación no fija ha de estar vacío.");
                        resultadoDebug.Añadida = false;
                    }
                    // Validación cruzada: El CP debe coincidir con la provincia declarada.
                    if (!string.IsNullOrWhiteSpace(dato.PROVINCIA) && !territoriosValidos.Contains(dato.PROVINCIA))
                    {
                        resultadoDebug.Motivos.Add("Provincia no válida.");
                        resultadoDebug.Añadida = false;
                    }
                    else if (!string.IsNullOrWhiteSpace(dato.PROVINCIA) && !CodigoPostalValido(dato.C_POSTAL, dato.PROVINCIA) && dato.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Motivos.Add($"Código postal {dato.C_POSTAL} no coincide con provincia '{dato.PROVINCIA}'.");
                        resultadoDebug.Añadida = false;
                    }
                    // Validación de prefijo de CP (debe ser 12, 46 o 03).
                    if (dato.C_POSTAL.Length >= 2)
                    {
                        var cpPrefijo = dato.C_POSTAL.Substring(0, 2);
                        var prefijosValidos = prefijosCpPorTerritorio.Values.ToHashSet();
                        if (!prefijosValidos.Contains(cpPrefijo))
                        {
                            resultadoDebug.Motivos.Add("El prefijo del código postal no coincide con Castellón, Valencia o Alicante.");
                            resultadoDebug.Añadida = false;
                        }
                    }

                    double lat = (double)dato.Latitud;
                    double lon = (double)dato.Longitud;

                    // Determinación del tipo (Fija, Móvil, Otros)
                    TipoEstacion tipo = TipoEstacion.Estacion_fija;
                    if (dato.TIPO_ESTACION != null)
                    {
                        if (dato.TIPO_ESTACION.Contains("Móvil", StringComparison.OrdinalIgnoreCase)) tipo = TipoEstacion.Estacion_movil;
                        else if (dato.TIPO_ESTACION.Contains("Agrícola", StringComparison.OrdinalIgnoreCase) ||
                                 !dato.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase)) tipo = TipoEstacion.Otros;
                    }

                    // Validación de coordenadas (si es fija, debe tenerlas y ser válidas)
                    if (lat == 0 && lon == 0 && tipo == TipoEstacion.Estacion_fija)
                    {
                        resultadoDebug.Añadida = false;
                        resultadoDebug.Motivos.Add("No se pudieron obtener las coordenadas (Selenium falló o no encontró).");
                    }
                    if (!EsCoordenadaEnEspañaPeninsular(lat, lon) && tipo == TipoEstacion.Estacion_fija)
                    {
                        resultadoDebug.Motivos.Add($"Coordenadas fuera de España peninsular ({lat}, {lon}).");
                        resultadoDebug.Añadida = false;
                    }

                    // Generación de nombre descriptivo para debug
                    if (tipo == TipoEstacion.Estacion_fija)
                    {
                        resultadoDebug.Nombre = dato.MUNICIPIO + " " + dato.Nº_ESTACION;
                    }
                    else if (tipo == TipoEstacion.Estacion_movil)
                    {
                        resultadoDebug.Nombre = "Móvil " + dato.Nº_ESTACION;
                    }
                    else if (dato.TIPO_ESTACION.Contains("Agrícola", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Nombre = "Agrícola " + dato.Nº_ESTACION;
                    }
                    else
                    {
                        resultadoDebug.Nombre = "Otro " + dato.Nº_ESTACION;
                    }

                    string nombreNormalizado = resultadoDebug.Nombre.Trim();
                    string coordsKey = $"{lat:F6},{lon:F6}";

                    // Detección de Duplicados Internos (mismo fichero)
                    if (!nombresEnEsteFichero.Add(nombreNormalizado))
                    {
                        resultadoDebug.Motivos.Add($"Nombre repetido dentro del archivo CV ({nombreNormalizado}).");
                        resultadoDebug.Añadida = false;
                    }
                    if (lat != 0 && lon != 0 && !coordenadasEnEsteFichero.Add(coordsKey))
                    {
                        resultadoDebug.Motivos.Add($"Coordenadas repetidas dentro del archivo CV ({lat:F6}, {lon:F6}).");
                        resultadoDebug.Añadida = false;
                    }

                    // Detección de Duplicados en Base de Datos
                    if (contexto.Estaciones.Any(e => e.nombre.ToLower() == nombreNormalizado.ToLower()))
                    {
                        resultadoDebug.Motivos.Add($"Nombre ya existe en la base de datos ({nombreNormalizado}).");
                        resultadoDebug.Añadida = false;
                    }
                    if (lat != 0 && lon != 0 && UbicacionRepetida(contexto, lat, lon))
                    {
                        resultadoDebug.Motivos.Add($"Ubicación ya usada en la base de datos ({lat:F6}, {lon:F6}).");
                        resultadoDebug.Añadida = false;
                    }

                    // Si se ha marcado como no válida por alguno de los motivos anteriores, saltamos.
                    if (!resultadoDebug.Añadida)
                    {
                        debugResultados.Add(resultadoDebug);
                        continue;
                    }

                    // === Solo si se añade, registramos la reparación de provincia ===
                    if (provinciaReparada)
                    {
                        resultadoDebug.Reparada = true;
                        resultadoDebug.Reparaciones.Add($"Para arreglar la provincia, esta se normalizó, pasándola de {provinciaOriginal} a {dato.PROVINCIA}.");
                        resultadoDebug.Motivos.Add("Provincia incorrecta: " + provinciaOriginal + ".");
                    }

                    // Gestión de relaciones DB: Obtener o crear entidades Provincia y Localidad
                    var provincia = ObtenerOCrearProvincia(contexto, dato.PROVINCIA);
                    var localidad = ObtenerOCrearLocalidad(contexto, dato.MUNICIPIO, provincia);
                    resultadoDebug.Provincia = provincia.nombre;

                    var horario = dato.HORARIOS ?? "Sin horario";

                    // Creación final de la entidad Estacion
                    var estacion = new Estacion
                    {
                        nombre = resultadoDebug.Nombre,
                        tipo = tipo,
                        direccion = dato.DIRECCION,
                        codigoPostal = dato.C_POSTAL,
                        latitud = lat,
                        longitud = lon,
                        descripcion = "",
                        horario = horario,
                        contacto = $"Correo electrónico: {dato.CORREOS}",
                        URL = "https://www.sitval.com/",
                        localidad = localidad,
                        codigoLocalidad = localidad.codigo
                    };

                    estacionesValidas.Add(estacion);
                    resultados.Add(new ResultObject { Estacion = estacion, Localidad = localidad, Provincia = provincia });
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

            // Inserción en bloque (Bulk Insert) para mejorar el rendimiento
            if (estacionesValidas.Any())
            {
                contexto.Estaciones.AddRange(estacionesValidas);
                contexto.SaveChanges();
            }

            MostrarResumen(debugResultados);
            int agregadas = resultados.Count;

            // Generación de logs de texto para la API
            string estacionesReparadas = string.Join("\n",
                debugResultados.Where(r => r.Reparada && r.Añadida)
                    .Select(r => $"{{{r.Fuente}, {r.Nombre}, {r.Municipio}, [{string.Join("; ", r.Motivos)}], [{string.Join("; ", r.Reparaciones)}]}}"));

            string estacionesRechazadas = string.Join("\n",
                debugResultados.Where(r => !r.Añadida)
                    .Select(r => $"{{{r.Fuente}, {r.Nombre}, {r.Municipio}, [{string.Join("; ", r.Motivos)}]}}"));

            return (resultados, agregadas, estacionesReparadas, estacionesRechazadas);
        }

        private bool EstacionYaExiste(AppDbContext ctx, string numeroEstacion)
        {


            return ctx.Estaciones.Any(e => e.nombre.Contains(numeroEstacion));
        }
        private bool EsCorreo(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            return Regex.IsMatch(texto, @"^[^\s@]+@[^\s@]+\.[^\s@]+$");
        }

        // Comprueba si ya existe una estación en las mismas coordenadas con una pequeña tolerancia
        private bool UbicacionRepetida(AppDbContext ctx, double lat, double lon)
        {


            const double tolerancia = 0.0001; // ~11 metros
            return ctx.Estaciones.Any(e => Math.Abs(e.latitud - lat) < tolerancia && Math.Abs(e.longitud - lon) < tolerancia);
        }

        private bool EsCoordenadaEnEspañaPeninsular(double lat, double lon)
        {
            const double latMin = 36.0, latMax = 43.8;
            const double lonMin = -9.3, lonMax = 3.3;
            return lat >= latMin && lat <= latMax && lon >= lonMin && lon <= lonMax;
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

        // Orquesta la carga de datos de la Comunidad Valenciana llamando a la API Wrapper y procesando el JSON.
        public async Task<(List<ResultObject>, int, string, string)> LoadData()
        {
            Debug.WriteLine("[CVExtractor] === INICIO CARGA COMUNIDAD VALENCIANA ===");
            try
            {
                // Llamada a la API Wrapper externa
                var response = await _http.GetAsync("/cv/json");
                Debug.WriteLine($"[CVExtractor] Respuesta HTTP: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    return (new List<ResultObject>(), 0, "", "ERROR CV HTTP");
                }


                // Lectura del contenido JSON
                var JsonCV = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[CVExtractor] Recibido JSON de {JsonCV.Length} caracteres");

                // Invocación a LoadFromString para deserializar y enriquecer datos
                this.LoadFromString(JsonCV);


                // Procesamiento ETL de los objetos ya enriquecidos
                Debug.WriteLine($"[CVExtractor] objetosParseados devolvió {objetosParseados.Count} objetos");

                var resultados = this.FromParsedToUsefull(objetosParseados);
                Debug.WriteLine($"[CVExtractor] === FIN CARGA: {resultados.Item1.Count} estaciones añadidas ===");
                return resultados; // Devuelve directamente la tupla
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepcion cargando datos de CV {ex.Message}");
                return (new List<ResultObject>(), 0, "", "ERROR CV");
            }
        }

        // Deserializa una cadena JSON y aplica el enriquecimiento de coordenadas mediante Selenium.
        public override void LoadFromString(string json)
        {

            Debug.WriteLine("[CVExtractor] LoadFromString iniciado");

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var listaObjetos = JsonSerializer.Deserialize<List<JSONData>>(json, options);

                if (listaObjetos == null || listaObjetos.Count == 0)
                {
                    Debug.WriteLine("[CVExtractor] JSON vacío o inválido → no se procesa nada");
                    objetosParseados = new List<JSONData>();
                    return;
                }

                Debug.WriteLine($"[CVExtractor] Parseados {listaObjetos.Count} objetos del JSON original");

                // Obtención coordenadas de las estaciones mediante Selenium (Scraping de Google Maps)
                ApplySelenium(listaObjetos);

                // Guardamos en variable de instancia para usarla en otros metodos
                objetosParseados = listaObjetos;


                Debug.WriteLine("[CVExtractor] Objetos modificados con coordenadas y listos para procesar");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRITICAL ERROR] LoadFromString falló: {ex.Message}");
                objetosParseados = new List<JSONData>();
            }
        }

        // Recorre la lista de datos y, para las estaciones fijas, utiliza Selenium para obtener coordenadas geográficas.
        private static void ApplySelenium(List<JSONData> elementos)
        {
            Debug.WriteLine($"[CVExtractor] Aplicando Selenium a {elementos.Count} elementos");

            var selenium = CoordenadasSelenium.Instance;

            // Si Selenium no está disponible (ej: falta driver), ponemos todo a 0 para no romper el flujo
            if (!selenium.Disponible)
            {
                Debug.WriteLine("[SELENIUM] No disponible → todas las coordenadas a (0,0)");
                foreach (var e in elementos)
                {
                    e.Latitud = 0;
                    e.Longitud = 0;
                }
                return;
            }

            foreach (var elemento in elementos)
            {
                // Solo buscamos coordenadas para Estaciones Fijas (las móviles no tienen ubicación estática)
                bool esEstacionFija = elemento.TIPO_ESTACION != null &&
                                      elemento.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase);

                if (esEstacionFija)
                {
                    Debug.WriteLine($"[SELENIUM] Obteniendo coordenadas para: {elemento.DIRECCION ?? "sin dirección"}, {elemento.MUNICIPIO ?? "sin municipio"}");

                    try
                    {

                        var coords = selenium.ObtenerCoordenadas(elemento.DIRECCION ?? "", elemento.MUNICIPIO ?? "");
                        elemento.Latitud = coords.Lat;
                        elemento.Longitud = coords.Lng;

                        if (coords.Lat != 0 || coords.Lng != 0)
                        {
                            Debug.WriteLine($"[SELENIUM] ÉXITO → ({coords.Lat}, {coords.Lng})");
                        }
                        else
                        {
                            Debug.WriteLine("[SELENIUM] No encontradas → usando (0,0)");
                            elemento.Latitud = 0.0;
                            elemento.Longitud = 0.0;
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