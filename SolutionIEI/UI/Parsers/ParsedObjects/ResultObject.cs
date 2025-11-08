using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Parsers.ParsedObjects 
{ 
    /// <summary>
    ///  Objeto representando la estructura de datos final antes de meterla en la base de datos
    /// </summary>
    public struct ResultObject
    {
        public UI.Entidades.Estacion Estacion { get; set; }
        public UI.Entidades.Localidad Localidad { get; set; }
        public UI.Entidades.Provincia Provincia { get; set; }
    }
}
