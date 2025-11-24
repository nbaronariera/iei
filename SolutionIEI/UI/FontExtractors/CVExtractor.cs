using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using UI.Entidades;
using UI.Helpers;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    public class CVExtractor : Parser<JSONData>
    {
        protected override List<JSONData> ExecuteParse()
        {
            if (file == null) return new List<JSONData>();
            string contenido = new StreamReader(file, Encoding.UTF8).ReadToEnd();
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<JSONData>>(contenido, opciones) ?? new List<JSONData>();
        }

        public List<ResultObject> FromParsedToUsefull(List<JSONData> datosParseados)
        {
            var resultados = new List<ResultObject>();
            using var contexto = new AppDbContext();
            using var seleniumHelper = new CoordenadasSelenium();

            foreach (var dato in datosParseados)
            {
                try
                {
                    var provincia = ObtenerOCrearProvincia(contexto, dato.PROVINCIA);
                    var localidad = ObtenerOCrearLocalidad(contexto, dato.MUNICIPIO, provincia);

                    double lat = 0, lon = 0;
                    string direccionBusqueda = dato.DIRECCION;
                    if (direccionBusqueda.Contains("I.T.V. Móvil") || direccionBusqueda.Contains("I.T.V. Agrícola"))
                    {
                        direccionBusqueda = dato.MUNICIPIO;
                    }

                    if (!contexto.Estaciones.Any(e => e.nombre == (string.IsNullOrWhiteSpace(dato.MUNICIPIO) ? dato.DIRECCION : dato.MUNICIPIO) && e.direccion == dato.DIRECCION))
                    {
                        var coords = seleniumHelper.ObtenerCoordenadas(direccionBusqueda, dato.MUNICIPIO);
                        lat = coords.Lat;
                        lon = coords.Lng;
                    }

                    TipoEstacion tipo = TipoEstacion.Estacion_fija;
                    if (dato.TIPO_ESTACION.Contains("Móvil", StringComparison.OrdinalIgnoreCase)) tipo = TipoEstacion.Estacion_movil;
                    else if (dato.TIPO_ESTACION.Contains("Agrícola", StringComparison.OrdinalIgnoreCase)) tipo = TipoEstacion.Otros;

                    var estacion = new Estacion
                    {
                        nombre = string.IsNullOrWhiteSpace(dato.MUNICIPIO) ? dato.DIRECCION : dato.MUNICIPIO,
                        tipo = tipo,
                        direccion = dato.DIRECCION,
                        codigoPostal = dato.C_POSTAL,
                        latitud = lat,
                        longitud = lon,
                        descripcion = "",
                        horario = dato.HORARIOS,
                        contacto = $"Correo electrónico: {dato.CORREOS} Teléfono: -",
                        URL = "https://www.sitval.com/",
                        localidad = localidad,
                        codigoLocalidad = localidad.codigo
                    };

                    contexto.Estaciones.Add(estacion);
                    resultados.Add(new ResultObject { Estacion = estacion, Localidad = localidad, Provincia = provincia });
                }
                catch { }
            }
            contexto.SaveChanges();
            return resultados;
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
    }
}