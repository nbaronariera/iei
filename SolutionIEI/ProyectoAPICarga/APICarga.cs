using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Entidades;
using UI.Helpers;
using UI.Logica;

namespace ProyectoAPICarga
{
    [ApiController]
    [Route("/carga/")]
    public class APICarga : ControllerBase
    {
        private readonly LogicaCarga _logica;
        public APICarga(LogicaCarga logica) => _logica = logica;

        /// <summary>
        /// Carga en la base de datos las estaciones de la Comunidad Valenciana
        /// </summary>
        /// 
        ///<returns> Devuelve una LoadResponse con los datos de la carga </returns> 
        ///<response code="200">Retorna los datos de la carga</response>
        [HttpPost("cv")]
        [ProducesResponseType(typeof(LoadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoadCV()
        {
            Debug.WriteLine("[API CARGA] INICIO LoadCV");
            try
            {
                Debug.WriteLine("[API CARGA] Llamando a LogicaCarga.ObtenerCV()");
                var resultado = await _logica.ObtenerCV();

                var respuesta = new LoadResponse(resultado.Item2, resultado.Item3, resultado.Item4);

                Debug.WriteLine("[API CARGA] FIN LoadCV OK");
                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                string errorMsg = $"ERROR al cargar la Comunidad Valenciana: {ex.Message}";
                if (ex.InnerException != null)
                    errorMsg += $"\nInner: {ex.InnerException.Message}";
                Debug.WriteLine($"[API] ERROR en LoadCV: {ex}");
                return StatusCode(500, errorMsg);
            }
        }

        /// <summary>
        /// Limpia la base de datos
        /// </summary>
        /// 
        ///<returns> Devuelve un booleando indicando si ha ido bien </returns> 
        ///<response code="200">Retorna true</response>
        [HttpDelete("delete")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Delete()
        {
            try
            {
                var resultado = _logica.Clean();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                string errorMsg = $"ERROR al borrar la base de datos: {ex.Message}";
                if (ex.InnerException != null)
                    errorMsg += $"\nInner: {ex.InnerException.Message}";
                Debug.WriteLine($"[API] ERROR en Delete: {ex}");
                return StatusCode(500, errorMsg);
            }
        }

        /// <summary>
        /// Carga en la base de datos las estaciones de Cataluña
        /// </summary>
        /// 
        ///<returns> Devuelve una LoadResponse con los datos de la carga </returns> 
        ///<response code="200">Retorna los datos de la carga</response>
        [HttpPost("cat")]
        [ProducesResponseType(typeof(LoadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoadCat()
        {
            Debug.WriteLine("[API CARGA] INICIO LoadCat");
            try
            {
                Debug.WriteLine("[API CARGA] Llamando a LogicaCarga.ObtenerCat()");
                var resultado = await _logica.ObtenerCat();

                var respuesta = new LoadResponse(resultado.Item2, resultado.Item3, resultado.Item4);

                Debug.WriteLine("[API CARGA] FIN LoadCat OK");
                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                string errorMsg = $"ERROR al cargar Cataluña: {ex.Message}";
                if (ex.InnerException != null)
                    errorMsg += $"\nInner: {ex.InnerException.Message}";
                Debug.WriteLine($"[API] ERROR en LoadCat: {ex}");
                return StatusCode(500, errorMsg);
            }
        }

        /// <summary>
        /// Carga en la base de datos las estaciones de Galicia
        /// </summary>
        /// 
        ///<returns> Devuelve una LoadResponse con los datos de la carga </returns> 
        ///<response code="200">Retorna los datos de la carga</response>
        [HttpPost("gal")]
        [ProducesResponseType(typeof(LoadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoadGal()
        {
            Debug.WriteLine("[API CARGA] INICIO LoadGal");
            try
            {
                Debug.WriteLine("[API CARGA] Llamando a LogicaCarga.ObtenerGal()");
                var resultado = await _logica.ObtenerGal();

                var respuesta = new LoadResponse(resultado.Item2, resultado.Item3, resultado.Item4);

                Debug.WriteLine("[API CARGA] FIN LoadGal OK");
                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                string errorMsg = $"ERROR al cargar Galicia: {ex.Message}";
                if (ex.InnerException != null)
                    errorMsg += $"\nInner: {ex.InnerException.Message}";
                Debug.WriteLine($"[API] ERROR en LoadGal: {ex}");
                return StatusCode(500, errorMsg);
            }
        }
    }
}