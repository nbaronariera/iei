using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI
{
    public class EstacionParaMostrar
    {
        [DisplayName("Nombre")]
        public string nombre { get; set; } = "";

        [DisplayName("Tipo")]
        public string Tipo { get; set; } = "";

        [DisplayName("Dirección")]
        public string direccion { get; set; } = "";

        [DisplayName("Provincia")]
        public string Provincia { get; set; } = "";

        [DisplayName("Localidad")]
        public string Localidad { get; set; } = "";

        [DisplayName("Código Postal")]
        public string CP { get; set; } = "";

        [DisplayName("Descripción")]
        public string descripcion { get; set; } = "";

        [DisplayName("Horario")]
        public string horario { get; set; } = "";

        [DisplayName("Contacto")]
        public string contacto { get; set; } = "";

        [DisplayName("Sitio web")]
        public string URL { get; set; } = "";

        [DisplayName("Latitud")]
        public double latitud { get; set; }

        [DisplayName("Longitud")]
        public double longitud { get; set; }
    }
}
