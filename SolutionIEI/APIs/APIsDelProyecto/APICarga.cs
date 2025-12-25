// Controllers/APIBusqueda.cs
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Entidades;
using UI.Logica;

namespace APIs.Controllers
{
    [ApiController]
    [Route("/carga")] 
    public class APICarga : ControllerBase
    {
        private readonly LogicaCarga _logica;
        public APICarga(LogicaCarga logica) => _logica = logica;

        /// <summary>
        /// Carga en la base de datos las estaciones de la Comunidad Valenciana
        /// </summary>
        /// 
        ///<returns> Devuelve el log de carga</returns> 
        ///<response code="200">Retorna el log de carga</response>
        [HttpPost("cv")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult LoadCV()
        {
            try
            {
                var lista = _logica.ObtenerCV();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[API] ERROR en GetCV: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Limpia la base de datos
        /// </summary>
        /// 
        ///<returns> Devuelve un booleano indicando si se ha vaciado correctamente</returns> 
        ///<response code="200">Retorna un booleano indicando si se ha vaciado correctamente</response>
        [HttpDelete("delete")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Delete()
        {
            try
            {
                var lista = _logica.Clean();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[API] ERROR en GetCV: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Carga en la base de datos las estaciones de Cataluña
        /// </summary>
        /// 
        ///<returns> Devuelve el log de carga</returns> 
        ///<response code="200">Retorna el log de carga</response>
        [HttpPost("cat")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult LoadCat()
        {
            try
            {
                var lista = _logica.ObtenerCat();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[API] ERROR en GetCat: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Carga en la base de datos las estaciones de Galicia
        /// </summary>
        /// 
        ///<returns> Devuelve el log de carga</returns> 
        ///<response code="200">Retorna el log de carga</response>
        [HttpPost("gal")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult LoadGal()
        {
            try
            {
                var lista = _logica.ObtenerGal();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[API] ERROR en GetGal: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}