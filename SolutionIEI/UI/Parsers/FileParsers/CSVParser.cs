using CsvHelper;
using System.Globalization;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    public class CSVParser : Parser<CSVData>
    {

        protected override List<CSVData> ExecuteParse()
        {
            using var reader = new StreamReader(file!);

            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                BadDataFound = null,
                MissingFieldFound = null,
                TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                IgnoreBlankLines = true
            };

            using var csv = new CsvReader(reader, config);
            return csv.GetRecords<CSVData>().ToList();
        }
    }
}
