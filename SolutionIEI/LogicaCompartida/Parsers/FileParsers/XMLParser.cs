using System.Xml.Serialization;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    internal class XMLParser : Parser<XMLData>
    {
        protected override List<XMLData> ExecuteParse()
        {
            XmlSerializer serializer = new(typeof(XMLResponse));
            var v = serializer.Deserialize(file!);

            return (v as XMLResponse).Wrapper.Rows;
        }
    }
}
