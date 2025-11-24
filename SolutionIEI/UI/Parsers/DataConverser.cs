using UI.Entidades;
using UI.Helpers;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    public class JSONConverter
    {
        public List<ResultObject> ConvertirLista(List<JSONData> datosJson)
        {
            var listaResultado = new List<ResultObject>();

            var selenium = new CoordenadasSelenium();

            try
            {
                foreach (var item in datosJson)
                {
                    double lat = 0.0;
                    double lng = 0.0;

                    // CASO A: Vienen del XML o CSV (Ya tienen coordenadas en las propiedades ocultas)
                    if (item.Latitud.HasValue && item.Longitud.HasValue && item.Latitud != 0)
                    {
                        lat = item.Latitud.Value;
                        lng = item.Longitud.Value;
                    }
                    // CASO B: Vienen del JSON de Valencia (No tienen coordenadas)
                    else if (!string.IsNullOrWhiteSpace(item.DIRECCION) &&
                             !string.IsNullOrWhiteSpace(item.MUNICIPIO))
                    {
                        (lat, lng) = selenium.ObtenerCoordenadas(item.DIRECCION, item.MUNICIPIO);
                    }

                    TipoEstacion tipoEnum;
                    string tipoTexto = item.TIPO_ESTACION?.ToLower() ?? "";

                    if (tipoTexto.Contains("fija"))
                        tipoEnum = TipoEstacion.Estacion_fija;
                    else if (tipoTexto.Contains("móvil") || tipoTexto.Contains("movil"))
                        tipoEnum = TipoEstacion.Estacion_movil;
                    else
                        tipoEnum = TipoEstacion.Otros;

                    // 3. Crear los objetos finales para la BD
                    var provincia = new Provincia
                    {
                        nombre = item.PROVINCIA ?? "Desconocida"
                    };

                    var localidad = new Localidad
                    {
                        nombre = !string.IsNullOrEmpty(item.MUNICIPIO) ? item.MUNICIPIO : "Sin Municipio"
                    };

                    var estacion = new Estacion
                    {
                        nombre = $"ITV {localidad.nombre} ({item.Nº_ESTACION})",
                        tipo = tipoEnum,
                        direccion = item.DIRECCION,
                        codigoPostal = item.C_POSTAL,
                        horario = item.HORARIOS,
                        contacto = item.CORREOS,
                        URL = "https://www.sitval.com/",
                        latitud = lat,
                        longitud = lng
                    };

                    var result = new ResultObject
                    {
                        Provincia = provincia,
                        Localidad = localidad,
                        Estacion = estacion
                    };

                    listaResultado.Add(result);

                    System.Diagnostics.Debug.WriteLine($"[Procesada] {estacion.nombre} -> Coords: {lat}, {lng}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR CRÍTICO] {ex.Message}");
            }
            finally
            {
                // Importante: Cerrar Chrome al terminar
                selenium.Dispose();
            }

            return listaResultado;
        }
    }
}
