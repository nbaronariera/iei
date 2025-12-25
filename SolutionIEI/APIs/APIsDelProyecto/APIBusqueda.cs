// Controllers/APIBusqueda.cs
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Entidades;
using UI.Logica;

namespace APIs.Controllers
{
    [ApiController]
    [Route("/")] // rutas directas: /provincias, /localidades, /estaciones
    [Produces("application/json")]
    public class APIBusqueda : ControllerBase
    {
        private readonly LogicaBusqueda _logica;
        public APIBusqueda(LogicaBusqueda logica) => _logica = logica;

        /// <summary>
        /// Devuelve todas las provincias disponibles.
        /// </summary>
        /// 
        ///<returns> Devuelve una lista de Provincias</returns> 
        ///<response code="200">Retorna la lista de Provincias</response>
        [HttpGet("provincias")]
        [ProducesResponseType(typeof(List<Provincia>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetProvincias()
        {
            try
            {
                var lista = _logica.ObtenerProvincias();
                Debug.WriteLine($"[API] GetProvincias → {lista.Count} provincias devueltas");
                return Ok(lista);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[API] ERROR en GetProvincias: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Obtiene las localidades.
        /// </summary>
        /// 
        ///<returns> Devuelve una lista de Localidad</returns> 
        ///<response code="200">Retorna la lista de Localidades</response>
        [HttpGet("localidades")]
        [ProducesResponseType(typeof(List<Localidad>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetLocalidades()
        {
            try
            {
                var lista = _logica.ObtenerLocalidades();
                Debug.WriteLine($"[API] GetLocalidades → {lista.Count} localidades devueltas");
                return Ok(lista);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[API] ERROR en GetLocalidades: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }


        /// <summary>
        /// Obtiene estaciones filtradas por CP, provincia, localidad y tipo.
        /// </summary>
        /// 
        /// <param name="cp">El cdigo postal de la estacion. Opcional</param>
        /// <param name="provincia">La provincia de la estacion. Opcional</param>
        /// <param name="localidad">La localidad de la estacion. Opcional</param>
        /// <param name="tipo">El tipo de la estacion. Opcional</param>
        /// 
        ///<returns> Devuelve una lista de Estaciones</returns> 
        ///<response code="200">Retorna la lista de Estaciones</response>
        [HttpGet("estaciones")]
        [ProducesResponseType(typeof(List<Estacion>), StatusCodes.Status200OK)]
        public IActionResult GetEstaciones(
          [FromQuery] string? cp,
          [FromQuery] string? provincia,
          [FromQuery] string? localidad,
          [FromQuery] string? tipo)
        {
            // Convertimos explícitamente null → cadena vacía
            cp ??= "";
            provincia ??= "";
            localidad ??= "";
            tipo ??= "";

            var lista = _logica.ObtenerEstaciones(cp, provincia, localidad, tipo);
            Debug.WriteLine($"[API] GetEstaciones → {lista.Count} estaciones devueltas (cp='{cp}', prov='{provincia}', loc='{localidad}', tipo='{tipo}')");
            return Ok(lista);
        }
    }
}