using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Parsers.ParsedObjects
{
    /// <summary>
    ///  Objeto representando la estructura de datos del archivo XML
    /// </summary>
    [XLMRot row]
    public struct XMLData
    {
        [XMLElement("estaci")]
        public string estaci;
        [XMLElement("denominaci")]
        public string denominaci;
        [XMLElement("operador")]
        public string operador;
        [XMLElement("adre_a")]
        public string adre_a;
        [XMLElement("cp")]
        public string cp;
        [XMLElement("municipi")]
        public string municipi;
        [XMLElement("codi_municipi")]
        public string codi_municipi
        [XMLElement("lat")]
        public string lat;
        [XMLElement("long")]
        public string long_coord;
        [XMLElement("geocoded_column")]
        public string geocoded_column;
        [XMLElement("localitzador_a_google_maps")]
        public string localitzador_a_google_maps;
        [XMLElement("serveis_territorials")]
        public string serveis_territorials;
        [XMLElement("horari_de_servei")]
        public string horari_de_servei;
        [XMLElement("correu_electr_nic")]
        public string correu_electr_nic;
         [XMLElement("web")]
        public string web;
    }
}
