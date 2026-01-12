using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Entidades;
using UI.Helpers;
using UI.Logica;

namespace ProyectoAPICarga
{
    /// <summary>
    /// Controlador principal para la gestión del proceso ETL (Extracción, Transformación y Carga).
    /// Permite disparar la carga de datos de cada comunidad y limpiar la base de datos.
    /// </summary>
    [ApiController]
    [Route("/carga/")]
    public class APICarga : ControllerBase
    {
        private readonly LogicaCarga _logica;

        // Inyección de dependencias de la capa de lógica
        public APICarga(LogicaCarga logica) => _logica = logica;

        /// <summary>
        /// Ejecuta el proceso ETL para las estaciones de la Comunidad Valenciana.
        /// </summary>
        /// <remarks>
        /// Este endpoint realiza los siguientes pasos:
        /// 1. Conecta con el Wrapper de CV para obtener el JSON crudo.
        /// 2. Parsea y sanitiza los datos (corrige CPs, asigna coordenadas).
        /// 3. Inserta los registros válidos en la base de datos.
        /// </remarks>
        /// <returns>Un objeto JSON con el resumen de registros insertados, reparados y rechazados.</returns>
        /// <response code="200">Proceso finalizado correctamente.</response>
        /// <response code="500">Error crítico durante la carga.</response>
        [HttpPost("cv")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoadCV()
        {
            Debug.WriteLine("[API CARGA] INICIO LoadCV");
            try
            {
                // Delegamos la lógica pesada a la capa de negocio
                Debug.WriteLine("[API CARGA] Llamando a LogicaCarga.ObtenerCV()");
                var resultado = await _logica.ObtenerCV();

                // Construimos una respuesta anónima con las estadísticas
                var respuesta = new
                {
                    RegistrosCargados = resultado.Item2,
                    RegistrosReparados = resultado.Item3 ?? "",
                    RegistrosRechazados = resultado.Item4 ?? ""
                };

                Debug.WriteLine("[API CARGA] FIN LoadCV OK");
                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                // Manejo de errores para no tirar el servidor
                var errorResponse = new
                {
                    RegistrosCargados = 0,
                    RegistrosReparados = "",
                    RegistrosRechazados = $"ERROR al cargar Comunidad Valenciana: {ex.Message}"
                };
                Debug.WriteLine($"[API] ERROR en LoadCV: {ex}");
                return StatusCode(500, errorResponse);
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
        public async Task<IActionResult> Delete()
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
                Console.WriteLine($"[API] ERROR en Delete: {ex}");
                return StatusCode(500, errorMsg);
            }
        }

        /// <summary>
        /// Ejecuta la carga ETL para las estaciones de Cataluña.
        /// </summary>
        /// <returns>Objeto JSON con estadísticas de la carga.</returns>
        /// <response code="200">Carga finalizada correctamente.</response>
        /// <response code="500">Error interno del servidor.</response>
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
                    RegistrosReparados = resultado.Item3 ?? "",
                    RegistrosRechazados = resultado.Item4 ?? ""
                };

                Debug.WriteLine("[API CARGA] FIN LoadCat OK");
                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    RegistrosCargados = 0,
                    RegistrosReparados = "",
                    RegistrosRechazados = $"ERROR al cargar Cataluña: {ex.Message}"
                };
                Debug.WriteLine($"[API] ERROR en LoadCat: {ex}");
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Ejecuta la carga ETL para las estaciones de Galicia.
        /// </summary>
        /// <returns>Objeto JSON con estadísticas de la carga.</returns>
        /// <response code="200">Carga finalizada correctamente.</response>
        /// <response code="500">Error interno del servidor.</response>
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
                    RegistrosReparados = resultado.Item3 ?? "",
                    RegistrosRechazados = resultado.Item4 ?? ""
                };

                Debug.WriteLine("[API CARGA] FIN LoadGal OK");
                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    RegistrosCargados = 0,
                    RegistrosReparados = "",
                    RegistrosRechazados = $"ERROR al cargar Galicia: {ex.Message}"
                };
                Debug.WriteLine($"[API] ERROR en LoadGal: {ex}");
                return StatusCode(500, errorResponse);
            }
        }
    }
}