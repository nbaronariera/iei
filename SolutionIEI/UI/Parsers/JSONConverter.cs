using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Entidades;
using UI.Parsers.ParsedObjects;
using UI.Helpers;


namespace UI.Parsers
{

    public class JSONConverter
    {

        public List<ResultObject> ConvertirLista(List<JSONData> datosJson)
        {
            var listaResultado = new List<ResultObject>();
            // Instanciamos Selenium UNA VEZ fuera del bucle
            var selenium = new CoordenadasSelenium();

            try
            {
                foreach (var item in datosJson)
                {
                    // 1. Obtener Coordenadas
                    double lat = 0.0;
                    double lng = 0.0;

                    if (!string.IsNullOrWhiteSpace(item.DIRECCION) && !string.IsNullOrWhiteSpace(item.MUNICIPIO))
                    {
                        (lat, lng) = selenium.ObtenerCoordenadas(item.DIRECCION, item.MUNICIPIO);
                    }

                    // 2. Convertir el string del JSON al Enum TipoEstacion
                    TipoEstacion tipoEnum;

                    // Convertimos a minúsculas para evitar problemas
                    string tipoTexto = item.TIPO_ESTACION?.ToLower() ?? "";

                    if (tipoTexto.Contains("fija"))
                    {
                        tipoEnum = TipoEstacion.Estacion_fija;
                    }
                    else if (tipoTexto.Contains("móvil") || tipoTexto.Contains("movil"))
                    {
                        tipoEnum = TipoEstacion.Estacion_movil;
                    }
                    else
                    {
                        // Para "Estación Agrícola" u otros casos
                        tipoEnum = TipoEstacion.Otros;
                    }

                    // 3. Crear las entidades
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
                        // AQUI USAMOS LA VARIABLE QUE HEMOS CONVERTIDO ARRIBA
                        tipo = tipoEnum,
                        direccion = item.DIRECCION,
                        codigoPostal = int.TryParse(item.C_POSTAL, out int cp) ? cp : 0,
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
                }
            }
            finally
            {
                selenium.Dispose();
            }
            return listaResultado;
        }
    }
}
