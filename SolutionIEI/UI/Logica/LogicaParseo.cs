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
        public List<GALData> loadGal()
        {
             
            string jsonContent = CSVaJSONConversor.Ejecutar(); // sigue generando y guardando el archivo
            return JsonSerializer.Deserialize<List<GALData>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public List<JSONData> loadCV()
        {
            string jsonContent = JSONaJSONConversor.Ejecutar();
            return JsonSerializer.Deserialize<List<JSONData>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public List<XMLData> loadCat()
        {
            string jsonContent = XMLaJSONConversor.Ejecutar();
            return JsonSerializer.Deserialize<List<XMLData>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
    }
}