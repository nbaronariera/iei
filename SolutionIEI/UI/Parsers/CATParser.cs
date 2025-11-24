using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UI.Entidades;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    public class CATParser : Parser<XMLData>
    {
        protected override List<XMLData> ExecuteParse()
        {
            if (file == null) return new List<XMLData>();

            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            string contenido = new StreamReader(file, Encoding.UTF8).ReadToEnd();

            return JsonSerializer.Deserialize<List<XMLData>>(contenido, opciones) ?? new List<XMLData>();
        }

        public List<ResultObject> FromParsedToUsefull(List<XMLData> datosParseados)
        {
            var resultados = new List<ResultObject>();
            using var contexto = new AppDbContext();

            Debug.WriteLine($"[CAT] Iniciando procesamiento de {datosParseados.Count} registros.");

            foreach (var dato in datosParseados)
            {
                try
                {

                    if (string.IsNullOrWhiteSpace(dato.denominaci)) continue;

                    var provinciaNombre = !string.IsNullOrWhiteSpace(dato.serveis_territorials)
                                          ? dato.serveis_territorials
                                          : "Desconocida";
                    var provincia = ObtenerOCrearProvincia(contexto, provinciaNombre);

                    var municipioNombre = !string.IsNullOrWhiteSpace(dato.municipi)
                                          ? dato.municipi
                                          : "Desconocido";
                    var localidad = ObtenerOCrearLocalidad(contexto, municipioNombre, provincia);


                    double lat = ParsearCoordenada(dato.lat);
                    double lon = ParsearCoordenada(dato.long_coord);


                    if (EstacionYaExiste(contexto, dato.denominaci, lat, lon))
                    {
                        Debug.WriteLine($"[CAT] Estación duplicada omitida: {dato.denominaci}");
                        continue;
                    }


                    string correoLimpio = EsUrl(dato.correu_electr_nic) ? "" : dato.correu_electr_nic;
                    string contactoFormateado = $"Correo electrónico: {correoLimpio} Teléfono: {dato.tel_atenc_public}";

                    // Creación de la entidad
                    var estacion = new Estacion
                    {
                        nombre = dato.denominaci,
                        tipo = TipoEstacion.Estacion_fija, // Valor fijo según PDF
                        direccion = dato.adre_a ?? "",
                        codigoPostal = ParsearInt(dato.cp),
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
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CAT] Error procesando estación {dato.denominaci}: {ex.Message}");
                }
            }

            contexto.SaveChanges();
            Debug.WriteLine($"[CAT] Carga finalizada. {resultados.Count} estaciones guardadas.");
            return resultados;
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

            string normalizado = coord.Replace(",", ".").Trim();
            if (double.TryParse(normalizado, NumberStyles.Any, CultureInfo.InvariantCulture, out double valor))
                return valor;
            return 0.0;
        }

        private int ParsearInt(string valor)
        {
            if (int.TryParse(valor, out int res)) return res;
            return 0;
        }

        private bool EsUrl(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            return texto.ToLower().StartsWith("http") || texto.ToLower().StartsWith("www");
        }


        private string ConvertirHorarioCAT(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";


            string horario = raw;

            horario = Regex.Replace(horario, "De dilluns a divendres", "L-V", RegexOptions.IgnoreCase);
            horario = Regex.Replace(horario, "Dilluns a divendres", "L-V", RegexOptions.IgnoreCase);
            horario = Regex.Replace(horario, "dissabtes?", "S", RegexOptions.IgnoreCase);
            horario = Regex.Replace(horario, "diumenges?", "D", RegexOptions.IgnoreCase);

            horario = horario.Replace(" h.", "").Replace(" h ", " ");

            return horario.Trim();
        }
    }
}