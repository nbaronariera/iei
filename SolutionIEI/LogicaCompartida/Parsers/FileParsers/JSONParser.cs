using System.IO;
using System.Text.Json;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    /// <summary>
    /// Clase encargada de la conversión y procesamiento de archivos o datos en formato JSON.
    /// Hereda de la clase base <see cref="Parser{T}"/>.
    /// </summary>
    internal class JSONParser : Parser<JSONData>
    {
        /// <summary>
        /// Convierte una lista de objetos <see cref="CSVData"/> a una cadena de texto en formato JSON.
        /// </summary>
        /// <param name="data">Lista de datos provenientes de un CSV que se desean serializar.</param>
        /// <returns>Una cadena (string) que representa los datos en formato JSON.</returns>
        public String toJSON(List<CSVData> data)
        {
            var test = JsonSerializer.Serialize<List<CSVData>>(data).ToString();
            return test;
        }

        /// <summary>
        /// Realiza la lectura del archivo configurado y deserta su contenido en una lista de objetos <see cref="JSONData"/>.
        /// </summary>
        /// <returns>
        /// Una lista de <see cref="JSONData"/> si el proceso es exitoso; 
        /// de lo contrario, devuelve una lista vacía.
        /// </returns>
        /// <remarks>
        /// Utiliza <see cref="JsonSerializerOptions"/> para permitir que la coincidencia de nombres de propiedades 
        /// no distinga entre mayúsculas y minúsculas.
        /// </remarks>
        protected override List<JSONData> ExecuteParse()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            // Lee el flujo del archivo hasta el final y lo deserializa
            // El uso de 'is not List<JSONData> res' actúa como una validación de seguridad (null-check)
            if (JsonSerializer.Deserialize<List<JSONData>>(new StreamReader(file!).ReadToEnd(), options) is not List<JSONData> res) { return new List<JSONData>(); }
            return res;
        }
    }
}
