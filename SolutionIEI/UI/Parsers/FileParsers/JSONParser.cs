using System.Text.Json;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    internal class JSONParser : Parser<JSONData>
    {
        public String toJSON(List<CSVData> data)
        {
            var test = JsonSerializer.Serialize<List<CSVData>>(data).ToString();
            return test;
        }

        protected override List<JSONData> ExecuteParse()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            if (JsonSerializer.Deserialize<List<JSONData>>(new StreamReader(file!).ReadToEnd(), options) is not List<JSONData> res) { return new List<JSONData>(); }
            return res;
        }
    }
}
