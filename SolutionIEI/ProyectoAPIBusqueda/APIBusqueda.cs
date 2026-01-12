// Controllers/APIBusqueda.cs
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Entidades;
using UI.Logica;

namespace ProyectoAPIBusqueda
{
    /// <summary>
    /// API pública de consulta. Provee endpoints para que aplicaciones cliente
    /// puedan recuperar listados de estaciones, provincias y localidades.
    /// </summary>
    [ApiController]
    [Route("/")] // rutas directas: /provincias, /localidades, /estaciones
    [Produces("application/json")]

    public class APIBusqueda : ControllerBase
    {
        private readonly LogicaBusqueda _logica;
        public APIBusqueda(LogicaBusqueda logica) => _logica = logica;

        /// <summary>
        /// Obtiene el listado de todas las provincias que tienen estaciones registradas.
        /// </summary>
        /// <returns>Lista de objetos Provincia.</returns>
        /// <response code="200">Retorna la lista correctamente.</response>
        [HttpGet("provincias")]
        [ProducesResponseType(typeof(List<Provincia>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetProvincias()
        {
            try
            {
                var lista = _logica.ObtenerProvincias();
                Console.WriteLine($"[API] GetProvincias → {lista.Count} provincias devueltas");
                return Ok(lista);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] ERROR en GetProvincias: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Obtiene el listado de todas las localidades registradas.
        /// </summary>
        /// <returns>Lista de objetos Localidad.</returns>
        /// <response code="200">Retorna la lista correctamente.</response>
        [HttpGet("localidades")]
        [ProducesResponseType(typeof(List<Localidad>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetLocalidades()
        {
            try
            {
                var lista = _logica.ObtenerLocalidades();
                Console.WriteLine($"[API] GetLocalidades → {lista.Count} localidades devueltas");
                return Ok(lista);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] ERROR en GetLocalidades: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }


        /// <summary>
        /// Obtiene estaciones filtradas por CP, provincia, localidad y tipo.
        /// </summary>
        /// <remarks>
        /// Los filtros funcionan de manera aditiva (AND). Si un parámetro se omite o es null, se ignora ese criterio.
        /// </remarks>
        /// <param name="cp">El cdigo postal de la estacion. Opcional</param>
        /// <param name="provincia">La provincia de la estacion. Opcional</param>
        /// <param name="localidad">La localidad de la estacion. Opcional</param>
        /// <param name="tipo">El tipo de la estacion. Opcional</param> 
        ///<returns> Devuelve una lista de Estaciones filtrada</returns> 
        ///<response code="200">Retorna la lista de Estaciones(puede estar vacía).</response>
        [HttpGet("estaciones")]
        [ProducesResponseType(typeof(List<Estacion>), StatusCodes.Status200OK)]
        public IActionResult GetEstaciones(
          [FromQuery] string? cp,
          [FromQuery] string? provincia,
          [FromQuery] string? localidad,
          [FromQuery] string? tipo)
        {
            // Sanitización: Convertimos explícitamente null a cadena vacía para evitar errores en la lógica
            cp ??= "";
            provincia ??= "";
            localidad ??= "";
            tipo ??= "";

            var lista = _logica.ObtenerEstaciones(cp, provincia, localidad, tipo);
            Console.WriteLine($"[API] GetEstaciones → {lista.Count} estaciones devueltas (cp='{cp}', prov='{provincia}', loc='{localidad}', tipo='{tipo}')");
            return Ok(lista);
        }
    }
}