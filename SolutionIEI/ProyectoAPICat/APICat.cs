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
                return StatusCode(500, "ERROR interno la intentar leer o convertir el archivo fuente de Cataluña");
            }
        }
    }
}