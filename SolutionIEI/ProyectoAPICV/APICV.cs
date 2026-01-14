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
        /// Obtiene el JSON crudo con los datos de las estaciones (sin procesar por el extractor de la comunidad) de la Comunidad Valenciana.
        /// </summary>
        /// <remarks>
        /// Este endpoint no realiza ninguna conversión de formatos al ya estar el archivo fuente en formato JSON y por ello únicamente lo devuelve.
        /// Ejemplo de respuesta (200 OK):
        /// <code>
        /// [
        ///   {
        ///     "TIPO ESTACIÓN": "Estación Fija",
        ///     "PROVINCIA": "Valencia",
        ///     "MUNICIPIO": "Utiel",
        ///     "C.POSTAL": 46300,
        ///     "DIRECCIÓN": "Pol. Ind. El Melero. Avda. de La Industria, Parcelas88y 89",
        ///     "Nº ESTACIÓN": 4605,
        ///     "HORARIOS": "L.V. 7:00-21:00",
        ///     "CORREO": "itv@"
        ///   },
        ///   {
        ///     "TIPO ESTACIÓN": "Estación Móvil",
        ///     "PROVINCIA": "Castellón",
        ///     "MUNICIPIO": "",
        ///     "C.POSTAL": "999999",
        ///     "DIRECCIÓN": "I.T.V. Móvil 01",
        ///     "Nº ESTACIÓN": 1251,
        ///     "HORARIOS": "variable según población",
        ///     "CORREO": "itv1251@sitval.com"
        ///   }
        /// ]
        /// </code>
        /// </remarks>
        /// <returns>(Si la operación es exitosa) Un archivo JSON con los datos de las estaciones de la Comunidad Valenciana</returns>
        /// <response code="200">Operación exitosa.</response>
        /// <response code="500">Error interno del servidor al intentar leer el archivo fuente de la Comunidad Valenciana (indicado al cliente mediante un mensaje de error).</response>
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
                return StatusCode(500, "ERROR interno la intentar leer el archivo fuente de la Comunidad Valenciana");
            }
        }
    }
}