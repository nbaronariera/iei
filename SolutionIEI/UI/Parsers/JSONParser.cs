using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    internal class JSONParser : Parser<JSONData>
    {
        protected override List<ResultObject> FromParsedToUsefull(List<JSONData> parsed)
        {
            throw new NotImplementedException();
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
