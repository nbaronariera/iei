using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Entidades;
using UI.Parsers;
using UI.Parsers.ParsedObjects;
using UI.Wrappers;

namespace UI.Logica
{
    public class LogicaParseo
    {
        public string loadCat() {
            return XMLaJSONConversor.Ejecutar();
        }

        public string loadCV()
        {
            return JSONCoordenadas.Ejecutar();
        }

        public string loadGal()
        {

            return CSVaJSONConversor.Ejecutar();
        }
    }
}