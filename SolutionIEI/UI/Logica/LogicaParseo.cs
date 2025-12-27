using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UI.Entidades;
using UI.Parsers;
using UI.Parsers.ParsedObjects;
using UI.Wrappers;

namespace UI.Logica
{
    public class LogicaParseo
    {
        public string loadGal()
        {
            return CSVaJSONConversor.Ejecutar(); 
        }

        public string loadCV()
        {
            return JSONConversor.Ejecutar();
        }

        public string loadCat()
        {
            return XMLaJSONConversor.Ejecutar();
        }
    }
}