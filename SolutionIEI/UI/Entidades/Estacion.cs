using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Entidades
{
    public class Estacion
    {
        public int cod_estacion { get; set; }
        public String nombre { get; set; } = "";
        public TipoEstacion tipo { get; set; }
        public String direccion { get; set; } = "";
        public int codigoPostal { get; set; }
        public double longitud  { get; set; }
        public double latitud { get; set; }
        public String descripcion { get; set; } = "";
        public String horario { get; set; } = "";
        public String contacto { get; set; } = "";
        public String URL { get; set; } = "";

        public Localidad localidad { get; set; } = null!;
        public int codigoLocalidad { get; set; }


        public Estacion(String nombre, TipoEstacion tipo, String direccion, int codigoPostal, double longitud, double latitud,
        String horario, String contacto, String URL)
        {

            this.nombre = nombre;
            this.tipo = tipo;
            this.direccion = direccion;
            this.codigoPostal = codigoPostal;
            this.longitud = longitud;
            this.latitud = latitud;
            this.horario = horario;
            this.contacto = contacto;
            this.URL = URL;
        }

        public Estacion() { }
    }

    public enum TipoEstacion
    {
        Estacion_fija,
        Estacion_movil,
        Otros
    }

    
}
