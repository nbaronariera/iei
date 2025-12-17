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

        [HttpPost("cv")]
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

        [HttpDelete("delete")]
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

        [HttpPost("cat")]
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

        [HttpPost("gal")]
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