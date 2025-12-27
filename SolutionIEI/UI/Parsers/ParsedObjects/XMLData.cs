using System.Xml;

namespace UI.Parsers.ParsedObjects
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
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
        [JsonPropertyName("estaci")]
        public string estaci { get; set; }

        [XmlElement("denominaci")]
        [JsonPropertyName("denominaci")]
        public string denominaci { get; set; }

        [XmlElement("operador")]
        [JsonPropertyName("operador")]
        public string operador { get; set; }

        [XmlElement("adre_a")]
        [JsonPropertyName("adre_a")]
        public string adre_a { get; set; }

        [XmlElement("cp")]
        [JsonPropertyName("cp")]
        public string cp { get; set; }

        [XmlElement("municipi")]
        [JsonPropertyName("municipi")]
        public string municipi { get; set; }

        [XmlElement("codi_municipi")]
        [JsonPropertyName("codi_municipi")]
        public string codi_municipi { get; set; }

        [XmlElement("tel_atenc_public")]
        [JsonPropertyName("tel_atenc_public")]
        public string tel_atenc_public { get; set; }

        [XmlElement("lat")]
        [JsonPropertyName("lat")]
        public string lat { get; set; }

        [XmlElement("long")]
        [JsonPropertyName("long")]
        public string long_coord { get; set; }

        [XmlElement("geocoded_column")]
        [JsonPropertyName("geocoded_column")]
        public string geocoded_column { get; set; }

        [XmlElement("localitzador_a_google_maps")]
        [JsonPropertyName("localitzador_a_google_maps")]
        public XmlLink localitzador_a_google_maps { get; set; }

        [XmlElement("serveis_territorials")]
        [JsonPropertyName("serveis_territorials")]
        public string serveis_territorials { get; set; }

        [XmlElement("horari_de_servei")]
        [JsonPropertyName("horari_de_servei")]
        public string horari_de_servei { get; set; }

        [XmlElement("correu_electr_nic")]
        [JsonPropertyName("correu_electr_nic")]
        public string correu_electr_nic { get; set; }

        [XmlElement("web")]
        [JsonPropertyName("web")]
        public XmlLink web { get; set; }


       
    }
}
