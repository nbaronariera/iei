using CsvHelper.Configuration.Attributes;
using System.Text.Json.Serialization;

namespace UI.Parsers.ParsedObjects
{
    public class GALData
    {
        [JsonPropertyName("NOME DA ESTACIÓN")]
        [Name("NOME DA ESTACIÓN")]
        public string NombreEstacion { get; set; } = "";

        [JsonPropertyName("ENDEREZO")]
        [Name("ENDEREZO")]
        public string Direccion { get; set; } = "";

        [JsonPropertyName("CONCELLO")]
        [Name("CONCELLO")]
        public string Municipio { get; set; } = "";

        [JsonPropertyName("CÓDIGO POSTAL")]
        [Name("CÓDIGO POSTAL")]
        public string CodigoPostal { get; set; } = "";

        [JsonPropertyName("PROVINCIA")]
        [Name("PROVINCIA")]
        public string Provincia { get; set; } = "";

        [JsonPropertyName("TELÉFONO")]
        [Name("TELÉFONO")]
        public string Telefono { get; set; } = "";

        [JsonPropertyName("HORARIO")]
        [Name("HORARIO")]
        public string HorarioRaw { get; set; } = "";

        [JsonPropertyName("SOLICITUDE DE CITA PREVIA")]
        [Name("SOLICITUDE DE CITA PREVIA")]
        public string UrlCita { get; set; } = "";

        [JsonPropertyName("CORREO ELECTRÓNICO")]
        [Name("CORREO ELECTRÓNICO")]
        public string Correo { get; set; } = "";

        [JsonPropertyName("COORDENADAS GMAPS")]
        [Name("COORDENADAS GMAPS")]
        public string Coordenadas { get; set; } = "";

        public override String ToString()
        {
            return $"{NombreEstacion} | {Provincia} | {Municipio} | {CodigoPostal} | {Direccion} | {Telefono} | {HorarioRaw} | {UrlCita} | {Correo} | {Coordenadas}";
        }
    }
}
