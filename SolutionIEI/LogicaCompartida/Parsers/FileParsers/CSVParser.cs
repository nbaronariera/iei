using CsvHelper;
using System.Globalization;
using System.IO;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    public class CSVParser : Parser<GALData>
    {

        protected override List<GALData> ExecuteParse()
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
            csv.Context.RegisterClassMap<GALDataMap>();
            return csv.GetRecords<GALData>().ToList();
        }
    }
}
