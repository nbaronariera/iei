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
        /// 1. Conecta con el Wrapper de CV para obtener el JSON crudo con todas las estaciones.
        /// 2. Parsea y sanitiza los datos (corrige CPs, asigna coordenadas...).
        /// 3. Inserta los registros válidos en la base de datos.
        /// </remarks>
        /// <returns>(Si la operación es exitosa) Un objeto JSON con la siguiente estructura: Primero, indica el número de estaciones de la Comunidad Valenciana cargadas,
        /// luego muestra con una cadena qué estaciones presentan erorres pero que se pudieron reparar, indicando qué errores tenían y cómo se arreglaron, y por último otra cadena con las estaciones que fueron rechazadas, 
        /// incluyendo los motivos por lo que no fueron cargadas.</returns>
        /// <response code="200">Operación exitosa.</response>
        /// <response code="500">Error interno del servidor al intentar cargar las estaciones de la Comunidad Valenciana (indicado al cliente mediante un mensaje de error).</response>
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
               
                Debug.WriteLine($"[API] ERROR en LoadCV: {ex}");
                return StatusCode(500, "ERROR interno al intentar cargar las estaciones de la Comunidad Valenciana");
            }
        }

        /// <summary>
        /// Limpia la base de datos
        /// </summary>
        /// 
        ///<returns> Devuelve un booleando indicando si ha ido bien </returns> 
        ///<response code="200">Operación exitosa (devuelve true)</response>
        ///<response code="500">Error interno del servidor al intentar limpiar la base de datos (devuelve false)</response>
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
                string errorMsg = $"ERROR interno al borrar la base de datos";
                if (ex.InnerException != null)
                    errorMsg += $"\nInner: {ex.InnerException.Message}";
                Console.WriteLine($"[API] ERROR en Delete: {ex}");
                return StatusCode(500, errorMsg);
            }
        }

        /// <summary>
        /// Ejecuta el proceso ETL para las estaciones de Cataluña.
        /// </summary>
        /// <remarks>
        /// Este endpoint realiza los siguientes pasos:
        /// 1. Conecta con el Wrapper de Cataluña para obtener el JSON crudo con todas las estaciones.
        /// 2. Parsea y sanitiza los datos (corrige CPs, asigna coordenadas...).
        /// 3. Inserta los registros válidos en la base de datos.
        /// </remarks>
        /// <returns>(Si la operación es exitosa) Un objeto JSON con la siguiente estructura: Primero, indica el número de estaciones de Cataluña cargadas,
        /// luego muestra con una cadena qué estaciones presentan erorres pero que se pudieron reparar, indicando qué errores tenían y cómo se arreglaron, y por último otra cadena con las estaciones que fueron rechazadas, 
        /// incluyendo los motivos por lo que no fueron cargadas.</returns>
        /// <response code="200">Operación exitosa.</response>
        /// <response code="500">Error interno del servidor al intentar cargar las estaciones de Cataluña (indicado al cliente mediante un mensaje de error).</response>
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
               
                Debug.WriteLine($"[API] ERROR en LoadCat: {ex}");
                return StatusCode(500, "ERROR interno al intentar cargar las estaciones de Cataluña");
            }
        }

        /// <summary>
        /// Ejecuta el proceso ETL para las estaciones de Galicia.
        /// </summary>
        /// <remarks>
        /// Este endpoint realiza los siguientes pasos:
        /// 1. Conecta con el Wrapper de Galicia para obtener el JSON crudo con todas las estaciones.
        /// 2. Parsea y sanitiza los datos (corrige CPs, asigna coordenadas...).
        /// 3. Inserta los registros válidos en la base de datos.
        /// </remarks>
        /// <returns>(Si la operación es exitosa) Un objeto JSON con la siguiente estructura: Primero, indica el número de estaciones de Galicia cargadas,
        /// luego muestra con una cadena qué estaciones presentan erorres pero que se pudieron reparar, indicando qué errores tenían y cómo se arreglaron, y por último otra cadena con las estaciones que fueron rechazadas, 
        /// incluyendo los motivos por lo que no fueron cargadas.</returns>
        /// <response code="200">Operación exitosa.</response>
        /// <response code="500">Error interno del servidor al intentar cargar las estaciones de Galicia (indicado al cliente mediante un mensaje de error).</response>
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
                
                Debug.WriteLine($"[API] ERROR en LoadGal: {ex}");
                return StatusCode(500, "ERROR interno al intentar cargar las estaciones de Galicia");
            }
        }
    }
}