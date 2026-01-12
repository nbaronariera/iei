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
        /// Obtiene el JSON crudo con los datos de las estaciones de Galicia.
        /// </summary>
        /// <remarks>
        /// Este endpoint realiza internamente la conversión del archivo fuente (CSV) a JSON
        /// y devuelve el contenido textual resultante. Es consumido por el proceso de carga (ETL).
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
                // Llamamos a la lógica para ejecutar la conversión CSV -> JSON y leer el resultado
                var lista = _logica.loadGal();
                // Devolvemos el contenido indicando explícitamente que es JSON (MIME type)
                return Content(lista, "application/json");
            }
            catch (Exception ex)
            {
                // Manejo de errores y logging en consola para depuración
                Console.WriteLine($"[API] ERROR en gal/GetJSON: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}