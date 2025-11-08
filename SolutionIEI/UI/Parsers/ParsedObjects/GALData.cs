using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UI.Parsers.ParsedObjects
{
    public class GALData
    {
        [JsonPropertyName("NOME DA ESTACI�N")] public string NombreEstacion { get; set; } = "";
        [JsonPropertyName("ENDEREZO")] public string Direccion { get; set; } = "";
        [JsonPropertyName("CONCELLO")] public string Municipio { get; set; } = "";
        [JsonPropertyName("C�DIGO POSTAL")] public string CodigoPostal { get; set; } = "";
        [JsonPropertyName("PROVINCIA")] public string Provincia { get; set; } = "";
        [JsonPropertyName("TEL�FONO")] public string Telefono { get; set; } = "";
        [JsonPropertyName("HORARIO")] public string HorarioRaw { get; set; } = "";
        [JsonPropertyName("SOLICITUDE DE CITA PREVIA")] public string UrlCita { get; set; } = "";
        [JsonPropertyName("CORREO ELECTR�NICO")] public string Correo { get; set; } = "";
        [JsonPropertyName("COORDENADAS GMAPS")] public string Coordenadas { get; set; } = "";
    }
}
