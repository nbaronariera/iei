// Controllers/APIBusqueda.cs
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Logica;

namespace APIs.Controllers
{
    [ApiController]
    [Route("/")] // rutas directas: /provincias, /localidades, /estaciones
    public class APIBusqueda : ControllerBase
    {
        private readonly LogicaBusqueda _logica;
        public APIBusqueda(LogicaBusqueda logica) => _logica = logica;

        [HttpGet("provincias")]
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

        [HttpGet("localidades")]
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

        [HttpGet("estaciones")]
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
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { mensaje = "API Búsqueda funcionando", puerto = 5001, hora = DateTime.Now });

        [HttpGet("diagnostico")]
        public IActionResult Diagnostico()
        {
            return Ok(new
            {
                Mensaje = "¡EL CONTROLADOR ESTÁ CARGADO Y RESPONDIENDO!",
                Puerto = "5001",
                Hora = DateTime.Now,
                Serializador = "Newtonsoft.Json activo (si ves esto, ya no hay JsonException)"
            });
        }
    }
}