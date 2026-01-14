// Controllers/APIBusqueda.cs
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Entidades;
using UI.Logica;
using UI.Parsers.ParsedObjects;

namespace ProyectoAPIGal
{
    /// <summary>
    /// API Wrapper para la comunidad de Galicia.
    /// Simula ser una fuente de datos externa que expone la información de las estaciones
    /// (originalmente en CSV) convertida a un formato JSON estándar para su consumo.
    /// </summary>
    [ApiController]
    [Route("/gal")]
 
    public class APIGal : ControllerBase
    {
        private readonly LogicaParseo _logica;
        // Inyección de la capa de lógica para delegar el procesamiento de archivos
        public APIGal(LogicaParseo logica) => _logica = logica;

        /// <summary>
        /// Obtiene el JSON crudo con los datos de las estaciones (sin procesar por el extractor de la comunidad) de Galicia.
        /// </summary>
        /// <remarks>
        /// Este endpoint procesa el archivo fuente original (CSV), lo convierte a JSON
        /// y devuelve el resultado.
        ///
        /// Ejemplo de respuesta (200 OK):
        /// <code>
        /// [
        ///   {
        ///     "NOME DA ESTACIÓN": "Estación ITV de Viveiro",
        ///     "ENDEREZO": "Rúa A Xunqueira, s/n",
        ///     "CONCELLO": "Viveiro",
        ///     "CÓDIGO POSTAL": "27850",
        ///     "PROVINCIA": "Lugo",
        ///     "TELÉFONO": "881 920 963",
        ///     "HORARIO": "de 8:30 a 14:00 e de 16:00 a 19:30 horas (de luns a venres) e de 8:00 a 14:30 horas (sábados)",
        ///     "SOLICITUDE DE CITA PREVIA": "https://www.sycitv.com/gl/cita-previa-particulares/?estacion=viveiro",
        ///     "CORREO ELECTRÓNICO": "viveiro@sycitv.com",
        ///     "COORDENADAS GMAPS": "43° 39.382', -7° 36.091'"
        ///   },
        ///   {
        ///     "NOME DA ESTACIÓN": "Estación ITV de Verín",
        ///     "ENDEREZO": "Polígono de Pazos - Parcela A-6",
        ///     "CONCELLO": "Verín",
        ///     "CÓDIGO POSTAL": "32600",
        ///     "PROVINCIA": "Ourense",
        ///     "TELÉFONO": "988 411 539 - 881 920 966",
        ///     "HORARIO": "de 8:30 a 14:00 e de 16:00 a 19:30 horas (de luns a venres) e de 8:00 a 14:30 horas (sábados)",
        ///     "SOLICITUDE DE CITA PREVIA": "https://www.sycitv.com/gl/cita-previa-particulares/?estacion=verin",
        ///     "CORREO ELECTRÓNICO": "verin@sycitv.com",
        ///     "COORDENADAS GMAPS": "41° 55.723', -7° 27.876'"
        ///   }
        /// ]
        /// </code>
        /// </remarks>
        /// <returns>(Si la operación es exitosa) Un archivo JSON con los datos de las estaciones de Galicia</returns>
        /// <response code="200">Operación exitosa.</response>
        /// <response code="500">Error interno del servidor al intentar leer o convertir el archivo fuente de Galicia (indicado al cliente mediante un mensaje de error).</response>
        [HttpGet("json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult getJSON()
        {
            try
            {
                // Llamamos a la lógica para ejecutar la conversión CSV -> JSON y leer el resultado
                var lista = _logica.loadGal();
                // Devolvemos el contenido indicando explícitamente que es JSON (MIME type)
                return Content(lista, "application/json");
            }
            catch (Exception ex)
            {
                // Manejo de errores y logging en consola para depuración
                Console.WriteLine($"[API] ERROR en gal/GetJSON: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, "ERROR interno al intentar leer o convertir el archivo fuente de Galicia");
            }
        }
    }
}