// Controllers/APIBusqueda.cs
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Entidades;
using UI.Logica;
using UI.Parsers.ParsedObjects;

namespace ProyectoAPICV
{
    /// <summary>
    /// API Wrapper para la Comunidad Valenciana.
    /// Simula ser una fuente de datos externa que sirve la información en formato JSON estandarizado.
    /// </summary>

    [ApiController]
    [Route("/cv")]
    
    public class APICV : ControllerBase
    {
        private readonly LogicaParseo _logica;
        public APICV(LogicaParseo logica) => _logica = logica;

        /// <summary>
        /// Obtiene el listado completo de estaciones de ITV de la Comunidad Valenciana.
        /// </summary>
        /// <remarks>
        /// Este endpoint lee el archivo fuente original (JSON), procesa posibles conversiones de formato si fuera necesario,
        /// y devuelve el contenido listo para ser consumido por el proceso de extracción.
        /// </remarks>
        /// <returns>Archivo JSON con los datos de las estaciones.</returns>
        /// <response code="200">Devuelve el JSON correctamente.</response>
        /// <response code="500">Si no se encuentra el archivo fuente o hay error de lectura.</response>
        [HttpGet("json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult getJSON()
        {
            try
            {
                // Llamamos a la lógica para cargar el archivo JSON de la Comunidad Valenciana
                var lista = _logica.loadCV();

                // Retorna el contenido con el Content-Type correcto para que el cliente lo interprete como JSON
                return Content(lista, "application/json");
            }
            catch (Exception ex)
            {
                // Manejo de errores y logging en consola para depuración
                Console.WriteLine($"[API] ERROR en cv/GetJSON: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}