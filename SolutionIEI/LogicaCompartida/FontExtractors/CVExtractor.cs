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
        private List<JSONData> objetosParseados = new List<JSONData>();

      

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
            var estacionesValidas = new List<Estacion>();

            var nombresEnEsteFichero = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var coordenadasEnEsteFichero = new HashSet<string>();



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




               
                

                resultadoDebug.Municipio = dato.MUNICIPIO;

                // Normalizar variantes ortográficas comunes (València -> Valencia)
                if (!string.IsNullOrWhiteSpace(dato.PROVINCIA) &&
                    dato.PROVINCIA.Trim().Equals("València", StringComparison.OrdinalIgnoreCase))
                {
                    dato.PROVINCIA = "Valencia";
                    resultadoDebug.Reparada = true;
                    resultadoDebug.Motivos.Add("Provincia incorrecta: València.");
                    resultadoDebug.Reparaciones.Add("Para arreglar la provincia, esta se normalizó, pasándola de València a Valencia.");
                }

                // Normalizar variantes ortográficas comunes (Alacant -> Alicante)
                if (!string.IsNullOrWhiteSpace(dato.PROVINCIA) &&
                    dato.PROVINCIA.Trim().Equals("Alacant", StringComparison.OrdinalIgnoreCase))
                {
                    dato.PROVINCIA = "Alicante";
                    resultadoDebug.Reparada = true;
                    resultadoDebug.Motivos.Add("Provincia incorrecta: Alacant.");
                    resultadoDebug.Reparaciones.Add("Para arreglar la provincia, esta se normalizó, pasándola de Alacant a Alicante.");
                }

                // Normalizar variantes ortográficas comunes (Castelló -> Castellón)
                if (!string.IsNullOrWhiteSpace(dato.PROVINCIA) &&
                    dato.PROVINCIA.Trim().Equals("Castelló", StringComparison.OrdinalIgnoreCase))
                {
                    dato.PROVINCIA = "Castellón";
                    resultadoDebug.Reparada = true;
                    resultadoDebug.Motivos.Add("Provincia incorrecta: Castelló.");
                    resultadoDebug.Reparaciones.Add("Para arreglar la provincia, esta se normalizó, pasándola de Castelló a Castellón.");
                }

                resultadoDebug.CodigoPostal = dato.C_POSTAL;

                try
                {
               
                    if (string.IsNullOrWhiteSpace(dato.PROVINCIA))
                    {
                        resultadoDebug.Motivos.Add("Provincia vacía.");
                        resultadoDebug.Añadida = false;
                    }

                    if (string.IsNullOrWhiteSpace(dato.MUNICIPIO) && dato.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Motivos.Add("Municipio vacío.");
                        resultadoDebug.Añadida = false;
                    }

                    if (!string.IsNullOrWhiteSpace(dato.MUNICIPIO) && !dato.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Motivos.Add("El municipio de una estación no fija ha de estar vacío.");
                        resultadoDebug.Añadida = false;
                    }

                    if (!Regex.IsMatch(dato.C_POSTAL, @"^\d{5}$") && dato.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Motivos.Add($"Código postal inválido ('{dato.C_POSTAL}'), al no tener 5 caracteres.");
                        resultadoDebug.Añadida = false;
                    }

                    if (!string.IsNullOrWhiteSpace(dato.C_POSTAL) && !dato.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Motivos.Add($"El código postal de una estación no fija ha de estar vacío.");
                        resultadoDebug.Añadida = false;
                    }

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

                    double lat = (double)dato.Latitud, lon = (double)dato.Longitud;

                    TipoEstacion tipo = TipoEstacion.Estacion_fija;
                    if (dato.TIPO_ESTACION != null)
                    {
                        if (dato.TIPO_ESTACION.Contains("Móvil", StringComparison.OrdinalIgnoreCase)) tipo = TipoEstacion.Estacion_movil;
                        else if (dato.TIPO_ESTACION.Contains("Agrícola", StringComparison.OrdinalIgnoreCase) || 
                            !dato.TIPO_ESTACION.Contains("Fija", StringComparison.OrdinalIgnoreCase)) tipo = TipoEstacion.Otros;
                        
                    }

                  

                    if (UbicacionRepetida(contexto,lat, lon) && tipo == TipoEstacion.Estacion_fija)
                    {
                        resultadoDebug.Motivos.Add($"Ubicación ya usada ({lat}, {lon}).");
                        resultadoDebug.Añadida = false;
                    }

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


                    // Procesamiento de Horario
                    var horario = dato.HORARIOS ?? "Sin horario";


                    if (tipo == TipoEstacion.Estacion_fija)
                    {
                        resultadoDebug.Nombre = dato.MUNICIPIO + " " + dato.Nº_ESTACION;
                    }
                    else if (tipo == TipoEstacion.Estacion_movil)
                    {
                        resultadoDebug.Nombre = "Móvil" + " " + dato.Nº_ESTACION;
                    }
                    else if (dato.TIPO_ESTACION.Contains("Agrícola", StringComparison.OrdinalIgnoreCase))
                    {
                        resultadoDebug.Nombre = "Agrícola" + " " + dato.Nº_ESTACION;
                    }
                    else
                    {
                        resultadoDebug.Nombre = "Otro" + " " + dato.Nº_ESTACION;
                    }

                    // En el foreach, después de todas las validaciones
                    string nombreNormalizado = resultadoDebug.Nombre.Trim();
                    string coordsKey = $"{lat:F6},{lon:F6}";

                    // Duplicados internos
                    if (!nombresEnEsteFichero.Add(nombreNormalizado))
                    {
                        resultadoDebug.Motivos.Add($"Nombre o número repetido dentro del archivo CV ({nombreNormalizado}).");
                        resultadoDebug.Añadida = false;
                    }
                    if (lat != 0 && lon != 0 && !coordenadasEnEsteFichero.Add(coordsKey))
                    {
                        resultadoDebug.Motivos.Add($"Coordenadas repetidas dentro del archivo CV ({lat:F6}, {lon:F6}).");
                        resultadoDebug.Añadida = false;
                    }

                    // Duplicados en BD
                    if (contexto.Estaciones.Any(e => string.Equals(e.nombre, nombreNormalizado, StringComparison.OrdinalIgnoreCase)))
                    {
                        resultadoDebug.Motivos.Add($"Nombre o número ya existe en la base de datos ({nombreNormalizado}).");
                        resultadoDebug.Añadida = false;
                    }
                    if (lat != 0 && lon != 0 && UbicacionRepetida(contexto, lat, lon))
                    {
                        resultadoDebug.Motivos.Add($"Ubicación ya usada en la base de datos ({lat:F6}, {lon:F6}).");
                        resultadoDebug.Añadida = false;
                    }

                    if (resultadoDebug.Añadida == false)
                    {

                        debugResultados.Add(resultadoDebug);
                        continue;
                    }



                    // Gestión de Base de Datos (Provincias y Localidades)
                    var provincia = ObtenerOCrearProvincia(contexto, dato.PROVINCIA);
                    var localidad = ObtenerOCrearLocalidad(contexto, dato.MUNICIPIO, provincia);

                   

                    resultadoDebug.Provincia = provincia.nombre;

                    // Creación del objeto Estacion
                    var estacion = new Estacion
                    {
                        nombre = resultadoDebug.Nombre,
                        tipo = tipo,
                        direccion = dato.DIRECCION,
                        codigoPostal = dato.C_POSTAL,
                        latitud = lat ,
                        longitud = lon,
                        descripcion = dato.TIPO_ESTACION ?? "",
                        horario = horario,
                        contacto = $"Correo electrónico: {dato.CORREOS}",
                        URL = "https://www.sitval.com/",
                        localidad = localidad,
                        codigoLocalidad = localidad.codigo
                    };

                    

                    estacionesValidas.Add(estacion);

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

            // Al final
            if (estacionesValidas.Any())
            {
                contexto.Estaciones.AddRange(estacionesValidas);
                contexto.SaveChanges();
            }

            contexto.SaveChanges();
            MostrarResumen(debugResultados);

            int agregadas = debugResultados.Count(r => r.Añadida);

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

        private bool UbicacionRepetida(AppDbContext ctx, double lat, double lon)
        {
         

            const double tolerancia = 0.0001; // ~11 metros
            return ctx.Estaciones.Any(e =>   Math.Abs(e.latitud - lat) < tolerancia && Math.Abs(e.longitud - lon) < tolerancia );
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

                //Obtención coordenadas de las estaciones mediante Selenium
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

        private static void ApplySelenium(List<JSONData> elementos)
        {
            Debug.WriteLine($"[CVExtractor] Aplicando Selenium a {elementos.Count} elementos");

            var selenium = CoordenadasSelenium.Instance;

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