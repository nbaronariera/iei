using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Entidades;

namespace UI.Formularios
{
    public static class UtilidadesFormularioBusqueda
    {
        ///<summary>
        ///Normaliza el nombre de una localidad
        ///Si el texto es nulo o "Cualquiera", lo transforma en un texto vacío 
        ///Si la localidad incluye el nombre de la provincia entre paréntesis elimina el texto entre estos
        ///</summary>
  
        public static string NormalizarLocalidadCombo(string texto)
        {
            if (string.IsNullOrEmpty(texto) || texto == "Cualquiera") return "";
            int pos = texto.IndexOf(" (");
            return pos > 0 ? texto.Substring(0, pos) : texto;
        }

        ///<summary>
        ///Devuelve el nombre de la provincia donde se ubica una localidad dado el nombre de esta
        ///</summary>
       
        public static string? ResolverProvinciaDesdeLocalidad(
            string localidad,
            List<Localidad> todas)
        {
            return todas
                .FirstOrDefault(l => l.nombre == localidad)?
                .Provincia?.nombre;
        }

        ///<summary>
        ///Dada una lista de estaciones, filtra las que deben mostrarse en el mapa
        ///descarta aquellas que no sean fijas
        ///</summary>
        public static List<EstacionParaMostrar> FiltrarParaMapa(
            List<EstacionParaMostrar> estaciones)
        {
            return estaciones
                .Where(e => e.Tipo == "Estación fija")
                .ToList();
        }
    }
}
