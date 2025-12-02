using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Entidades;

namespace UI
{
    public class LogicaUIBusqueda
    {
        private readonly Persistencia _persistencia;

        string TIPO_FIJA = TipoEstacion.Estacion_fija.ToString();
        string TIPO_MOVIL = TipoEstacion.Estacion_movil.ToString();

        public LogicaUIBusqueda()
        {
            _persistencia = new Persistencia();
        }

        // ============================================================
        // =========    Conversión común lista y mapa     =============
        // ============================================================
        private List<EstacionParaMostrar> BuscarEstacionesParaMostrar(
            string codPostal,
            string provincia,
            string localidad,
            string tipoEstacion)
        {
            var estaciones = _persistencia.ObtenerEstaciones();

            // ---- FILTRO TIPO ----
            if (tipoEstacion == "Estación fija")
                estaciones = estaciones.Where(e => e.tipo == TipoEstacion.Estacion_fija).ToList();
            else if (tipoEstacion == "Estación móvil")
                estaciones = estaciones.Where(e => e.tipo == TipoEstacion.Estacion_movil).ToList();
            else if (tipoEstacion == "Otros")
                estaciones = estaciones.Where(e => e.tipo == TipoEstacion.Otros).ToList();

            // ---- FILTRO CÓDIGO POSTAL ----
            if (!string.IsNullOrWhiteSpace(codPostal))
            {
                if (codPostal.EndsWith("000"))
                    return new List<EstacionParaMostrar>(); // regla especial

                estaciones = estaciones
                    .Where(e => e.codigoPostal == codPostal)
                    .ToList();
            }

            // ---- FILTRO PROVINCIA ----
            if (!string.IsNullOrWhiteSpace(provincia) && provincia != "Cualquiera")
            {
                estaciones = estaciones
                    .Where(e => e.localidad?.Provincia?.nombre == provincia)
                    .ToList();
            }

            // ---- FILTRO LOCALIDAD ----
            if (!string.IsNullOrWhiteSpace(localidad) && localidad != "Cualquiera")
            {
                string nombreLocalidad = localidad.Contains(" (")
                    ? localidad.Split(" (")[0]
                    : localidad;

                estaciones = estaciones
                    .Where(e => e.localidad?.nombre == nombreLocalidad)
                    .ToList();
            }

            // ---- Conversión a EstacionParaMostrar ----
            return estaciones.Select(e => new EstacionParaMostrar
            {
                nombre = e.nombre,
                Tipo = TraducirTipo(e.tipo.ToString()),
                direccion = e.direccion,
                Provincia = e.localidad?.Provincia?.nombre ?? "",
                Localidad = e.tipo == TipoEstacion.Estacion_fija ? e.localidad?.nombre ?? "" : "",
                CP = e.tipo == TipoEstacion.Estacion_fija ? e.codigoPostal ?? "" : "",
                descripcion = e.descripcion ?? "",
                horario = e.horario ?? "",
                contacto = e.contacto ?? "",
                URL = e.URL ?? "",
                latitud = e.latitud,
                longitud = e.longitud
            }).ToList();
        }

        // ============================================================
        // PÚBLICO → para el GRID
        // ============================================================
        public List<EstacionParaMostrar> BuscarEstacionesParaLista(
            string codPostal,
            string provincia,
            string localidad,
            string tipoEstacion)
        {
            return BuscarEstacionesParaMostrar(codPostal, provincia, localidad, tipoEstacion);
        }

        // ============================================================
        // PÚBLICO → para el MAPA
        // Solo devuelve estaciones fijas con lat/lon válidas
        // ============================================================
        public List<EstacionParaMostrar> BuscarEstacionesParaMapa(
            string codPostal,
            string provincia,
            string localidad,
            string tipoEstacion)
        {
            return BuscarEstacionesParaMostrar(codPostal, provincia, localidad, tipoEstacion)
                .Where(e =>
                    e.Tipo == "Estación fija")
                .ToList();
        }

        // ============================================================
        // ===============   PROVINCIAS PARA UI   =====================
        // ============================================================
        public List<string> ObtenerProvinciasParaCombo()
        {
            var provincias = _persistencia.ObtenerProvincias()
                .Select(p => p.nombre)
                .ToList();

            provincias.Insert(0, "Cualquiera");
            return provincias;
        }

        // ============================================================
        // ===============   LOCALIDADES PARA UI   ====================
        // ============================================================
        public List<string> ObtenerLocalidadesParaCombo()
        {
            var localidades = _persistencia.ObtenerLocalidades()
                .Where(l => l.nombre != "Agrícola" && l.nombre != "Móvil")
                .Select(l => $"{l.nombre} ({l.Provincia?.nombre})")
                .ToList();

            localidades.Insert(0, "Cualquiera");
            return localidades;
        }

        public List<string> ObtenerLocalidadesPorProvincia(string provincia)
        {
            var lista = _persistencia.ObtenerLocalidades()
                .Where(l => l.Provincia != null &&
                            l.Provincia.nombre == provincia &&
                            l.nombre != "Agrícola" &&
                            l.nombre != "Móvil")
                .Select(l => l.nombre)
                .ToList();

            lista.Insert(0, "Cualquiera");
            return lista;
        }

        public string TraducirTipo(string tipo)
        {
            if (tipo == TIPO_FIJA) return "Estación fija";
            if (tipo == TIPO_MOVIL) return "Estación móvil";
            return "Otros";
        }

        // ============================================================
        // LOCALIDAD → devuelve solo nombre
        // ============================================================
        public string NormalizarLocalidadCombo(string localidadCombo)
        {
            if (localidadCombo.Contains(" ("))
                return localidadCombo.Split(new[] { " (" }, StringSplitOptions.None)[0];

            return localidadCombo;
        }

        // ============================================================
        // LOCALIDAD → devuelve provincia asociada
        // ============================================================
        public string ResolverProvinciaDesdeLocalidad(string localidad)
        {
            var loc = _persistencia.ObtenerLocalidades()
                .FirstOrDefault(l => l.nombre == localidad);

            return loc?.Provincia?.nombre;
        }
    }
}
