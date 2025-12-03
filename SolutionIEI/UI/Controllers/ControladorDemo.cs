using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace UI.Controllers
{
    [ApiController]
    [Route("api/demo")]
    internal class ControladorDemo
    {
        [HttpGet]
        public IActionResult ObtenerTodos()
        {
            var _logica = new LogicaUIBusqueda();
            List<EstacionParaMostrar> elementos = _logica.BuscarEstacionesParaLista("", "", "", "");
            return new OkObjectResult(elementos);
        }
    }
}
