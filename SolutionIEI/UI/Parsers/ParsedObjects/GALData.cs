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
        public string NombreEstacion { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string Municipio { get; set; } = "";
        public string CodigoPostal { get; set; } = "";
        public string Provincia { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string HorarioRaw { get; set; } = "";
        public string UrlCita { get; set; } = "";
        public string Correo { get; set; } = "";
        public string Coordenadas { get; set; } = "";

        public String toString()
        {
            return NombreEstacion + "\n" + Direccion + "\n" + Municipio + "\n" + CodigoPostal + "\n" + Provincia + "\n" + Telefono + "\n" + HorarioRaw + "\n" + UrlCita + "\n" + Correo + "\n" + Coordenadas;
        }
    }
}
