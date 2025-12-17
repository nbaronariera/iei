// Controllers/APIBusqueda.cs
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
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
        /// <returns>Lista de provincias.</returns>
        [HttpGet("provincias")]
        [ProducesResponseType(StatusCodes.Status200OK)]
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
        /// Devuelve todas las localidades disponibles.
        /// </summary>
        /// <returns>Lista de localidades.</returns>
        [HttpGet("localidades")]
        [ProducesResponseType(StatusCodes.Status200OK)]
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
        [HttpGet("estaciones")]
        [ProducesResponseType(StatusCodes.Status200OK)]
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

        // ← AÑADE ESTE MÉTODO DE PRUEBA (para confirmar que funciona)
        /// <summary>
        /// Prueba de diagnóstico para confirmar que la API está funcionando.
        /// </summary>
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { mensaje = "API Búsqueda funcionando", puerto = 5001, hora = DateTime.Now });

        
       
    }
}