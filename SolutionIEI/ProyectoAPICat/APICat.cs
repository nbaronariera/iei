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
        /// Obtiene el JSON crudo con los datos de las estaciones de Cataluña.
        /// </summary>
        /// <remarks>
        /// Este endpoint procesa el archivo fuente original (XML), lo convierte a JSON
        /// y devuelve el resultado como texto plano para ser consumido por el extractor ETL.
        /// </remarks>
        /// <returns>Cadena de texto con el contenido JSON.</returns>
        /// <response code="200">Devuelve el archivo JSON correctamente.</response>
        /// <response code="500">Se produjo un error al leer o convertir el archivo fuente.</response>
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
                return StatusCode(500, ex.Message);
            }
        }
    }
}