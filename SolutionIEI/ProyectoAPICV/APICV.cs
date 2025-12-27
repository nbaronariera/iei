// Controllers/APIBusqueda.cs
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Entidades;
using UI.Logica;
using UI.Parsers.ParsedObjects;

namespace ProyectoAPICV
{
    [ApiController]
    [Route("/cv")]
    
    public class APICV : ControllerBase
    {
        private readonly LogicaParseo _logica;
        public APICV(LogicaParseo logica) => _logica = logica;

        /// <summary>
        /// Obtiene el json de la base de datos de la Comunidad Valenciana
        /// </summary>
        /// 
        ///<returns> Devuelve el json </returns> 
        ///<response code="200">Retorna el json</response>
        [HttpGet("json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult getJSON()
        {
            try
            {
                var lista = _logica.loadCV();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[API] ERROR en cv/GetJSON: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}