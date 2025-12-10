// Controllers/APIBusqueda.cs
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Logica;

namespace APIs.Controllers
{
    [ApiController]
    [Route("/carga")] 
    public class APICarga : ControllerBase
    {
        private readonly LogicaCarga _logica;
        public APICarga(LogicaCarga logica) => _logica = logica;

        [HttpGet("cv")]
        public IActionResult GetCV()
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

        [HttpDelete("clean")]
        public IActionResult Clean()
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

        [HttpGet("cat")]
        public IActionResult GetCat()
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

        [HttpGet("gal")]
        public IActionResult GetGal()
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