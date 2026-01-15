using CsvHelper;
using System.Globalization;
using System.IO;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    /// <summary>
    /// Implementación específica del parser para archivos CSV que transforma datos en objetos GALData (Estaciones ITV gallegas).
    /// </summary>
    public class CSVParser : Parser<GALData>
    {
        /// <summary>
        /// Ejecuta la lógica de lectura y mapeo del archivo CSV.
        /// </summary>
        /// <returns>Una lista de estaciones gallegas mapeadas.</returns>
        protected override List<GALData> ExecuteParse()
        {
            // Inicializa el lector de archivos utilizando el archivo proporcionado en la clase base
            using var reader = new StreamReader(file!);

            // Configuración personalizada para el lector CSV
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";", // Define el punto y coma como separador de columnas
                BadDataFound = null, // Ignora errores de datos mal formados para evitar excepciones
                MissingFieldFound = null, // Permite que falten campos en el archivo sin lanzar errores
                TrimOptions = CsvHelper.Configuration.TrimOptions.Trim, // Elimina espacios en blanco innecesarios
                IgnoreBlankLines = true // Salta las líneas que estén vacías
            };

            using var csv = new CsvReader(reader, config);

            // Registra el mapeo personalizado para indicar cómo relacionar las columnas CSV con las propiedades de GALData
            csv.Context.RegisterClassMap<GALDataMap>();

            // Convierte las filas del CSV en una lista de objetos de C# y la retorna
            return csv.GetRecords<GALData>().ToList();
        }
    }
}
