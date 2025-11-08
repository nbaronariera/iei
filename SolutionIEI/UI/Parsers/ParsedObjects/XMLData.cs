using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using UI.Parsers.ParsedObjects;
using System.Xml;

namespace UI.Parsers.ParsedObjects
{
    /// <summary>
    ///  Objeto representando la estructura de datos del archivo XML
    /// </summary>
    [XmlRoot ("row")]
    public struct XMLData
    {
        [XmlElement("estaci")]
        public string estaci;
        [XmlElement("denominaci")]
        public string denominaci;
        [XmlElement("operador")]
        public string operador;
        [XmlElement("adre_a")]
        public string adre_a;
        [XmlElement("cp")]
        public string cp;
        [XmlElement("municipi")]
        public string municipi;
        [XmlElement("codi_municipi")]
        public string codi_municipi;
        [XmlElement("lat")]
        public string lat;
        [XmlElement("long")]
        public string long_coord;
        [XmlElement("geocoded_column")]
        public string geocoded_column;
        [XmlElement("localitzador_a_google_maps")]
        public string localitzador_a_google_maps;
        [XmlElement("serveis_territorials")]
        public string serveis_territorials;
        [XmlElement("horari_de_servei")]
        public string horari_de_servei;
        [XmlElement("correu_electr_nic")]
        public string correu_electr_nic;
         [XmlElement("web")]
        public string web;
    }
}
