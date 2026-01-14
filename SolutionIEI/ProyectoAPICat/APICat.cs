// Controllers/APIBusqueda.cs
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Entidades;
using UI.Logica;
using UI.Parsers.ParsedObjects;

namespace ProyectoAPICat
{
    /// <summary>
    /// API Wrapper para la comunidad de Cataluña.
    /// Actúa como fachada de una fuente de datos externa, sirviendo la información de las estaciones
    /// (originalmente en XML) en un formato JSON estandarizado.
    /// </summary>
    [ApiController]
    [Route("/cat")]
 
    public class APICat : ControllerBase
    {
        private readonly LogicaParseo _logica;

        // Inyección de dependencias para acceder a la lógica de conversión de archivos
        public APICat(LogicaParseo logica) => _logica = logica;

        /// <summary>
        /// Obtiene el JSON crudo con los datos de las estaciones (sin procesar por el extractor de la comunidad) de Cataluña.
        /// </summary>
        /// <remarks>
        /// Este endpoint procesa el archivo fuente original (XML), lo convierte a JSON
        /// y devuelve el resultado.
        /// 
        /// Ejemplo de respuesta (200 OK):
        /// <code>
        /// [
        /// {
        /// "estaci": "B11",
        /// "denominaci": "Cornellà",
        /// "operador": "APPLUS ITV",
        /// "adre_a": "Passeig DE LA CAMPSA 64",
        /// "cp": null,
        /// "municipi": "Cornellà de Llobregat",
        /// "codi_municipi": "080734",
        /// "tel_atenc_public": "902930200",
        /// "lat": "41357138",
        /// "long": "2095921",
        /// "geocoded_column": "POINT (2095921 41357138)",
        /// "localitzador_a_google_maps": {
        /// "url": "http://maps.google.com/maps?t=k&amp;q=41.357138+2.095921"
        /// },
        /// "serveis_territorials": "Barcelona",
        /// "horari_de_servei": "De dilluns a dijous de 7 a 22h, divendres de 7 a 21h i dissabtes de 9 a 14h. (...)",
        /// "correu_electr_nic": "www.applusiteuve.com",
        /// "web": {
        /// "url": "http://www.applusiteuve.com"
        /// }
        /// },
        /// {
        /// "estaci": "B23",
        /// "denominaci": "BCN Caracas",
        /// "operador": "TuV-RHEINLAND",
        /// "adre_a": "c. Caracas, 10 B",
        /// "cp": "08030",
        /// "municipi": "Barcelona",
        /// "codi_municipi": "080193",
        /// "tel_atenc_public": "933457154",
        /// "lat": "95000000",
        /// "long": "200000000",
        /// "geocoded_column": "POINT (2203403 41441284)",
        /// "localitzador_a_google_maps": {
        /// "url": "http://maps.google.com/maps?t=k&amp;q=41.441284+2.203403"
        /// },
        /// "serveis_territorials": "Barcelona",
        /// "horari_de_servei": "De dilluns a divendres de 7h a 21h. Dissabte de 8h a 14h",
        /// "correu_electr_nic": "b23@certio.com",
        /// "web": {
        /// "url": "http://www.itv-tuvrheinland.es"
        /// }
        /// }
        /// ]
        /// </code>
        /// </remarks>
        /// <returns>(Si la operación es exitosa) Un archivo JSON con los datos de las estaciones de Cataluña</returns>
        /// <response code="200">Operación exitosa.</response>
        /// <response code="500">Error interno del servidor al intentar leer o convertir el archivo fuente de Cataluña (indicado al cliente mediante un mensaje de error).</response>
        [HttpGet("json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult getJSON()
        {
            try
            {
                // Delegamos en la capa lógica la lectura del XML y su transformación a JSON string
                var lista = _logica.loadCat();

                // Retornamos el string especificando el tipo de contenido para que el cliente lo interprete bien
                return Content(lista, "application/json");
            }
            catch (Exception ex)
            {
                // Captura de errores de I/O o de parseo
                Console.WriteLine($"[API] ERROR en cat/GetJSON: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, "ERROR interno al intentar leer o convertir el archivo fuente de Cataluña");
            }
        }
    }
}