// Controllers/APIBusqueda.cs
using LogicaCompartida.DTOs;
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
        /// <returns>(Si la operación es exitosa) Lista de objetos ProvinciaDTO serializada a JSON.</returns>
        /// <remarks>
        /// Ejemplo de respuesta (200 OK):
        /// <code>
        /// [
        ///   {"nombre":"A Coruña"},
        ///   {"nombre":"Alicante"},
        ///   {"nombre":"Barcelona"},
        ///   {"nombre":"Castellón"},
        ///   {"nombre":"Girona"},
        ///   {"nombre":"Lleida"},
        ///   {"nombre":"Lugo"},
        ///   {"nombre":"Ourense"},
        ///   {"nombre":"Pontevedra"},
        ///   {"nombre":"Tarragona"},
        ///   {"nombre":"Valencia"}
        /// ]
        /// </code>
        /// </remarks>
        /// <response code="200">Operación exitosa</response>
        /// <response code="500">Error interno del servidor al intentar obtener las provincias (indicado al cliente mediante un mensaje de error)</response>
        [HttpGet("provincias")]
        [ProducesResponseType(typeof(List<ProvinciaDTO>), StatusCodes.Status200OK)]
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
                return StatusCode(500, "Hubo un error interno del servidor al intentar devolver las provincias");
            }
        }

        /// <summary>
        /// Obtiene el listado de todas las localidades que tienen estaciones registradas.
        /// </summary>
        /// <returns>(Si la operación es exitosa) Lista de objetos LocalidadDTO serializada a JSON.</returns>
        /// <remarks>
        /// Ejemplo de respuesta (200 OK):
        /// <code>
        /// [
        ///   {"nombreLocalidad":"Arteixo","nombreProvincia":"A Coruña"},
        ///   {"nombreLocalidad":"Ribeira","nombreProvincia":"A Coruña"},
        ///   {"nombreLocalidad":"Sada","nombreProvincia":"A Coruña"},
        ///   {"nombreLocalidad":"Santiago de Compostela","nombreProvincia":"A Coruña"},
        ///   {"nombreLocalidad":"Orihuela","nombreProvincia":"Alicante"},
        ///   {"nombreLocalidad":"Redován","nombreProvincia":"Alicante"},
        ///   {"nombreLocalidad":"Villena","nombreProvincia":"Alicante"},
        ///   {"nombreLocalidad":"Barcelona","nombreProvincia":"Barcelona"},
        ///   {"nombreLocalidad":"Cornellà de Llobregat","nombreProvincia":"Barcelona"}
        /// ]
        /// </code>
        /// </remarks>
        /// <response code="200">Operación exitosa</response>
        /// <response code="500">Error interno del servidor al intentar obtener las localidades (indicado al cliente mediante un mensaje de error)</response>
        [HttpGet("localidades")]
        [ProducesResponseType(typeof(List<LocalidadDTO>), StatusCodes.Status200OK)]
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
                return StatusCode(500, "Hubo un error interno del servidor al intentar devolver las localidades");
            }
        }


        /// <summary>
        /// Obtiene las estaciones registradas filtradas por CP, provincia, localidad y/o tipo.
        /// </summary>
        /// <remarks>
        /// Los filtros funcionan de manera aditiva (AND). Si un parámetro se omite o es null, se ignora ese criterio.
        /// 
        /// Ejemplo de respuesta (200 OK):
        /// <code>
        /// [
        ///   {
        ///     "nombre":"Utiel 4605",
        ///     "tipo":"Estación fija",
        ///     "direccion":"Pol. Ind. El Melero. Avda. de La Industria, Parcelas88y 89",
        ///     "provincia":"Valencia",
        ///     "localidad":"Utiel",
        ///     "cp":"46300",
        ///     "descripcion":"",
        ///     "horario":"L.V. 7:00-21:00",
        ///     "contacto":"Correo electrónico: itv@",
        ///     "url":"https://www.sitval.com/",
        ///     "latitud":39.56667,
        ///     "longitud":-1.2
        ///   },
        ///   {
        ///     "nombre":"Alzira 4606",
        ///     "tipo":"Estación fija",
        ///     "direccion":"Ctra. Algemesí, s/n",
        ///     "provincia":"Valencia",
        ///     "localidad":"Alzira",
        ///     "cp":"46600",
        ///     "descripcion":"",
        ///     "horario":"L.V. 7:00-22:00 / S. 8:00-14:00",
        ///     "contacto":"Correo electrónico: itv4606@sitval.com",
        ///     "url":"https://www.sitval.com/",
        ///     "latitud":39.1761325,
        ///     "longitud":-0.4437992
        ///   }
        ///  
        /// ]
        /// </code>
        /// </remarks>
        /// <param name="cp">El cdigo postal de la estacion. Opcional</param>
        /// <param name="provincia">La provincia de la estacion. Opcional</param>
        /// <param name="localidad">La localidad de la estacion. Opcional</param>
        /// <param name="tipo">El tipo de la estacion. Opcional</param> 
        ///<returns>(Si la operación es exitosa) Lista de EstacionesDTO filtradas serializada a JSON</returns> 
        ///<response code="200">Operación exitosa.</response>
        /// <response code="500">Error interno del servidor al intentar obtener las estaciones (indicado al cliente mediante un mensaje de error)</response>
        [HttpGet("estaciones")]
        [ProducesResponseType(typeof(List<EstacionDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

            try
            {

                var lista = _logica.ObtenerEstaciones(cp, provincia, localidad, tipo);
                Console.WriteLine($"[API] GetEstaciones → {lista.Count} estaciones devueltas (cp='{cp}', prov='{provincia}', loc='{localidad}', tipo='{tipo}')");
                return Ok(lista);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] ERROR en GetLocalidades: {ex.Message}");
                return StatusCode(500, "Hubo un error interno del servidor al intentar devolver las estaciones");
            }
        }

    }
}