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
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("response")]
    public class XMLResponse
    {
        [XmlElement("row")]
        public RowWrapper Wrapper { get; set; }
    }

    public class RowWrapper
    {
        [XmlElement("row")]
        public List<XMLData> Rows { get; set; }
    }

    public class XmlLink
    {
        [XmlAttribute("url")]
        public string url { get; set; }
    }

    public class XMLData
    {
        [XmlElement("estaci")]
        public string estaci { get; set; }

        [XmlElement("denominaci")]
        public string denominaci { get; set; }

        [XmlElement("operador")]
        public string operador { get; set; }

        [XmlElement("adre_a")]
        public string adre_a { get; set; }

        [XmlElement("cp")]
        public string cp { get; set; }

        [XmlElement("municipi")]
        public string municipi { get; set; }

        [XmlElement("codi_municipi")]
        public string codi_municipi { get; set; }

        [XmlElement("tel_atenc_public")]
        public string tel_atenc_public { get; set; }

        [XmlElement("lat")]
        public string lat { get; set; }

        [XmlElement("long")]
        public string long_coord { get; set; }

        [XmlElement("geocoded_column")]
        public string geocoded_column { get; set; }

        [XmlElement("localitzador_a_google_maps")]
        public XmlLink localitzador_a_google_maps { get; set; }

        [XmlElement("serveis_territorials")]
        public string serveis_territorials { get; set; }

        [XmlElement("horari_de_servei")]
        public string horari_de_servei { get; set; }

        [XmlElement("correu_electr_nic")]
        public string correu_electr_nic { get; set; }

        [XmlElement("web")]
        public XmlLink web { get; set; }
    }
}
