using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace UI.Parsers.ParsedObjects
{
    public class CSVData
    {
        [Name("NOME DA ESTACIÓN")] public string NombreEstacion { get; set; } = "";
        [Name("ENDEREZO")] public string Direccion { get; set; } = "";
        [Name("CONCELLO")] public string Municipio { get; set; } = "";
        [Name("CÓDIGO POSTAL")] public string CodigoPostal { get; set; } = "";
        [Name("PROVINCIA")] public string Provincia { get; set; } = "";
        [Name("TELÉFONO")] public string Telefono { get; set; } = "";
        [Name("HORARIO")] public string HorarioRaw { get; set; } = "";
        [Name("SOLICITUDE DE CITA PREVIA")] public string UrlCita { get; set; } = "";
        [Name("CORREO ELECTRÓNICO")] public string Correo { get; set; } = "";
        [Name("COORDENADAS GMAPS")] public string Coordenadas { get; set; } = "";
    }
}
