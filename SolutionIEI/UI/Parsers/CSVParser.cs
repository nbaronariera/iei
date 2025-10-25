using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    public class CSVParser : Parser<CSVData>
    {
        protected override List<ResultObject> FromParsedToUsefull(List<CSVData> parsed)
        {
            throw new NotImplementedException();
        }

        protected override List<CSVData> ExecuteParse()
        {
            using var csv = new CsvReader(new StreamReader(file!), CultureInfo.InvariantCulture);
            return csv.GetRecords<CSVData>().ToList();
        }
    }
}
