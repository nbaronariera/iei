using System.Text.Json.Serialization;

namespace UI.Parsers.ParsedObjects
{
    /// <summary>
    ///  Objeto representando la estructura de datos del archivo JSON
    /// </summary>
    public class JSONData
    {
        [JsonPropertyName("TIPO ESTACIÓN")]
        public string TIPO_ESTACION { get; set; }

        [JsonPropertyName("PROVINCIA")]
        public string PROVINCIA { get; set; }

        [JsonPropertyName("MUNICIPIO")]
        public string MUNICIPIO { get; set; }

        [JsonPropertyName("C.POSTAL")]
        [JsonConverter(typeof(FlexibleConverter))]
        public string C_POSTAL { get; set; }

        [JsonPropertyName("DIRECCIÓN")]
        public string DIRECCION { get; set; }

        [JsonPropertyName("Nº ESTACIÓN")]
        [JsonConverter(typeof(FlexibleConverter))]
        public string Nº_ESTACION { get; set; }

        [JsonPropertyName("HORARIOS")]
        public string HORARIOS { get; set; }

        [JsonPropertyName("CORREO")]
        public string CORREOS { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Latitud { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Longitud { get; set; }

        public override string ToString()
        {
            return TIPO_ESTACION + "\n" + PROVINCIA + "\n" + MUNICIPIO + "\n" + C_POSTAL + "\n" + DIRECCION + "\n" + Nº_ESTACION + "\n" + HORARIOS + "\n" + CORREOS;
        }

        public string ToJSON()
        {
            // 1. Convertimos los números usando InvariantCulture para asegurar el PUNTO (.)
            string latStr = Latitud.HasValue
                ? Latitud.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : "null";

            string lonStr = Longitud.HasValue
                ? Longitud.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : "null";

            // 2. Construimos el JSON.
            string res =
                "\"TIPO ESTACIÓN\" : \"" + TIPO_ESTACION + "\",\n" +
                "\"PROVINCIA\" : \"" + PROVINCIA + "\",\n" +
                "\"MUNICIPIO\" : \"" + MUNICIPIO + "\",\n" +
                "\"C.POSTAL\" : \"" + C_POSTAL + "\",\n" +
                "\"DIRECCIÓN\" : \"" + DIRECCION + "\",\n" +
                "\"Nº ESTACIÓN\" : \"" + Nº_ESTACION + "\",\n" +
                "\"HORARIOS\" : \"" + HORARIOS + "\",\n" +
                "\"CORREO\" : \"" + CORREOS + "\",\n" +
                "\"Latitud\" : " + latStr + ",\n" +
                "\"Longitud\" : " + lonStr + "\n";

            return res;
        }
    }
}
