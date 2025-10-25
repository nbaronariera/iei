using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    internal class XMLParser : Parser<XMLData>
    {
        protected override List<ResultObject> FromParsedToUsefull(List<XMLData> parsed)
        {
            throw new NotImplementedException();
        }

        protected override List<XMLData> ExecuteParse()
        {
            XmlSerializer serializer = new(typeof(XMLData));
            if (serializer.Deserialize(file!) is not List<XMLData> res) { return new List<XMLData>(); }
            return res;
        }
    }
}
