using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Entidades;

namespace UI.Logica
{
    public class LogicaBusqueda
    {
        private readonly Persistencia.Persistencia _persistencia;

        private const string TIPO_FIJA = "Estacion_fija";
        private const string TIPO_MOVIL = "Estacion_movil";

        public LogicaBusqueda()
        {
            _persistencia = new Persistencia.Persistencia();
        }

        // ============================================================
        // ========= Conversión común lista y mapa ===================
        // ============================================================
        public List<EstacionParaMostrar> ObtenerEstaciones(
            string codPostal,
            string provincia,
            string localidad,
            string tipoEstacion)
        {
            var estaciones = _persistencia.ObtenerEstaciones();

            // Filtro por tipo
            if (tipoEstacion == "Estación fija")
                estaciones = estaciones.Where(e => e.tipo == TipoEstacion.Estacion_fija).ToList();
            else if (tipoEstacion == "Estación móvil")
                estaciones = estaciones.Where(e => e.tipo == TipoEstacion.Estacion_movil).ToList();
            else if (tipoEstacion == "Otros")
                estaciones = estaciones.Where(e => e.tipo == TipoEstacion.Otros).ToList();

            // Filtro código postal
            if (!string.IsNullOrWhiteSpace(codPostal))
            {
                if (codPostal.EndsWith("000"))
                    return new List<EstacionParaMostrar>();

                estaciones = estaciones.Where(e => e.codigoPostal == codPostal).ToList();
            }

            // Filtro provincia
            if (!string.IsNullOrWhiteSpace(provincia) && provincia != "Cualquiera")
                estaciones = estaciones.Where(e => e.localidad?.Provincia?.nombre == provincia).ToList();

            // Filtro localidad
            if (!string.IsNullOrWhiteSpace(localidad) && localidad != "Cualquiera")
                estaciones = estaciones.Where(e => e.localidad?.nombre == localidad).ToList();

            // Conversión final
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
        // =============== PROVINCIAS (DEVUELVE OBJETOS) =============
        // ============================================================
        public List<Provincia> ObtenerProvincias()
        {
            return _persistencia.ObtenerProvincias()
                .Select(p => new Provincia
                {
                    codigo = p.codigo,
                    nombre = p.nombre
                    // NO incluir Localidades → evita referencia circular
                })
                .OrderBy(p => p.nombre)
                .ToList();
        }

        // ============================================================
        // =============== LOCALIDADES (DEVUELVE OBJETOS) ============
        // ============================================================
        public List<Localidad> ObtenerLocalidades()
        {
            return _persistencia.ObtenerLocalidades()
                .Where(l => l.nombre != "Agrícola" && l.nombre != "Móvil")
                .Select(l => new Localidad
                {
                    codigo = l.codigo,
                    nombre = l.nombre,
                    codigoProvincia = l.codigoProvincia,
                    Provincia = l.Provincia != null ? new Provincia
                    {
                        codigo = l.Provincia.codigo,
                        nombre = l.Provincia.nombre
                    } : null
                    // NO incluir Estaciones → evita referencia circular
                })
                .OrderBy(l => l.Provincia?.nombre)
                .ThenBy(l => l.nombre)
                .ToList();
        }

        // ============================================================
        // ========== LOCALIDADES POR PROVINCIA (solo nombres) ========
        // ============================================================
        public List<string> ObtenerLocalidadesPorProvincia(string provincia)
        {
            var lista = _persistencia.ObtenerLocalidades()
                .Where(l => l.Provincia != null &&
                            l.Provincia.nombre == provincia &&
                            l.nombre != "Agrícola" &&
                            l.nombre != "Móvil")
                .Select(l => l.nombre)
                .OrderBy(n => n)
                .ToList();

            lista.Insert(0, "Cualquiera");
            return lista;
        }

        // ============================================================
        // ====================== UTILIDADES =========================
        // ============================================================
        public string TraducirTipo(string tipo)
        {
            return tipo switch
            {
                TIPO_FIJA => "Estación fija",
                TIPO_MOVIL => "Estación móvil",
                _ => "Otros"
            };
        }
    }
}