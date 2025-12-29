using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicaCompartida.DTOs
{
    public class LocalidadDTO
    {
        public string NombreLocalidad { get; set; } = string.Empty;           // Nombre de la localidad
        public string NombreProvincia { get; set; } = string.Empty;  // Nombre de la provincia (para filtro y construcción)
    }
}
