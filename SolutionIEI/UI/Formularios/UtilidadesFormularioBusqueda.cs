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
        public static string NormalizarLocalidadCombo(string texto)
        {
            if (string.IsNullOrEmpty(texto) || texto == "Cualquiera") return "";
            int pos = texto.IndexOf(" (");
            return pos > 0 ? texto.Substring(0, pos) : texto;
        }

        public static string? ResolverProvinciaDesdeLocalidad(
            string localidad,
            List<Localidad> todas)
        {
            return todas
                .FirstOrDefault(l => l.nombre == localidad)?
                .Provincia?.nombre;
        }

        public static List<EstacionParaMostrar> FiltrarParaMapa(
            List<EstacionParaMostrar> estaciones)
        {
            return estaciones
                .Where(e => e.Tipo == "Estación fija")
                .ToList();
        }
    }
}
