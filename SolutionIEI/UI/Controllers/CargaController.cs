using Microsoft.AspNetCore.Mvc;
using UI.Entidades;
using UI.Parsers;
using UI.Wrappers;

namespace UI.Controllers
{
    [ApiController]
    [Route("api/carga")]
    public class CargaController : ControllerBase
    {
        [HttpPost]
        public IActionResult Cargar()
        {
            try
            {
                // 1. Limpiar la BD
                using (var db = new AppDbContext())
                {
                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();
                }

                // 2. Ejecutar Conversores para obtener rutas
                string jsonCV = JSONConversor.Ejecutar();
                string jsonCAT = XMLaJSONConversor.Ejecutar();
                string jsonGAL = CSVaJSONConversor.Ejecutar();

                // 3. Ejecutar Parsers e Insertar en BD
                var cvParser = new CVExtractor();
                cvParser.Load(jsonCV);
                var resCV = cvParser.FromParsedToUsefull(cvParser.ParseList());

                var catParser = new CATExtractor();
                catParser.Load(jsonCAT);
                var resCAT = catParser.FromParsedToUsefull(catParser.ParseList());

                var galParser = new GALExtractor();
                galParser.Load(jsonGAL);
                var resGAL = galParser.FromParsedToUsefull(galParser.ParseList());

                // 4. Devolver resumen JSON
                return Ok(new
                {
                    mensaje = "Carga completada",
                    detalles = new
                    {
                        CV = $"Insertadas: {resCV.Item2}",
                        CAT = $"Insertadas: {resCAT.Item2}",
                        GAL = $"Insertadas: {resGAL.Item2}"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
