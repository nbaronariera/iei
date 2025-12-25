// Controllers/APIBusqueda.cs
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Entidades;
using UI.Logica;

namespace APIs.Controllers
{
    [ApiController]
    [Route("/cat")]
    public class APICat : ControllerBase
    {
        private readonly LogicaParseo _logica;
        public APICat(LogicaParseo logica) => _logica = logica;

        /// <summary>
        /// Obtiene el json de la base de datos de Cataluña
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
                var lista = _logica.loadCat();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[API] ERROR en cat/GetJSON: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}