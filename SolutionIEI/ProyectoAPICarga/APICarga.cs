using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Entidades;
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
        [HttpPost("cv")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoadCV()
        {
            Debug.WriteLine("[API CARGA] INICIO LoadCV");
            try
            {
                Debug.WriteLine("[API CARGA] Llamando a LogicaCarga.ObtenerCV()");
                var resultado = await _logica.ObtenerCV();

                var respuesta = new
                {
                    RegistrosCargados = resultado.Item2,
                    RegistrosReparados = resultado.Item3,
                    RegistrosRechazados = resultado.Item4
                };

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
        [HttpPost("cat")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoadCat()
        {
            Debug.WriteLine("[API CARGA] INICIO LoadCat");
            try
            {
                Debug.WriteLine("[API CARGA] Llamando a LogicaCarga.ObtenerCat()");
                var resultado = await _logica.ObtenerCat();

                var respuesta = new
                {
                    RegistrosCargados = resultado.Item2,
                    RegistrosReparados = resultado.Item3,
                    RegistrosRechazados = resultado.Item4
                };

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
        [HttpPost("gal")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoadGal()
        {
            Debug.WriteLine("[API CARGA] INICIO LoadGal");
            try
            {
                Debug.WriteLine("[API CARGA] Llamando a LogicaCarga.ObtenerGal()");
                var resultado = await _logica.ObtenerGal();

                var respuesta = new
                {
                    RegistrosCargados = resultado.Item2,
                    RegistrosReparados = resultado.Item3,
                    RegistrosRechazados = resultado.Item4
                };

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

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("API de carga funcionando correctamente en puerto 8081");
        }
    }
}