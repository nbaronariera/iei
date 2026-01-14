using LogicaCompartida.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Entidades;

namespace UI.Logica
{
    // Clase que encapsula las consultas a la base de datos (Data Access Layer).
    // Permite filtrar estaciones y obtener listas maestras de provincias/localidades.
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
        public List<EstacionDTO> ObtenerEstaciones(
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
                if (codPostal.EndsWith("000") || codPostal.Length != 5)
                    return new List<EstacionDTO>();
                estaciones = estaciones.Where(e => e.codigoPostal == codPostal).ToList();
            }

            // Filtro provincia
            if (!string.IsNullOrWhiteSpace(provincia))
                estaciones = estaciones.Where(e => e.localidad?.Provincia?.nombre == provincia).ToList();

            // Filtro localidad
            if (!string.IsNullOrWhiteSpace(localidad))
                estaciones = estaciones.Where(e => e.localidad?.nombre == localidad).ToList();

            // Conversión final con lógica de visibilidad
            return estaciones.Select(e => new EstacionDTO
            {
                nombre = e.nombre,
                Tipo = TraducirTipo(e.tipo.ToString()),
                // Solo mostrar dirección, CP, localidad, lat/long si es fija
                direccion = e.tipo == TipoEstacion.Estacion_fija ? e.direccion ?? "" : "",
                Provincia = e.localidad?.Provincia?.nombre ?? "",
                Localidad = e.tipo == TipoEstacion.Estacion_fija ? e.localidad?.nombre ?? "" : "",
                CP = e.tipo == TipoEstacion.Estacion_fija ? e.codigoPostal ?? "" : "",
                descripcion = e.descripcion ?? "",
                horario = e.horario ?? "",
                contacto = e.contacto ?? "",
                URL = e.URL ?? "",
                latitud = e.tipo == TipoEstacion.Estacion_fija ? e.latitud : 0,
                longitud = e.tipo == TipoEstacion.Estacion_fija ? e.longitud : 0
            }).ToList();
        }

        // ============================================================
        // =============== PROVINCIAS (DEVUELVE OBJETOS) =============
        // ============================================================
        public List<ProvinciaDTO> ObtenerProvincias()
        {
            return _persistencia.ObtenerProvincias()
                .Where(l => !l.nombre.Contains("Desconocida"))
                .Select(p => new ProvinciaDTO
                {
                    
                    Nombre = p.nombre
                 
                })
                .OrderBy(p => p.Nombre)
                .ToList();
        }

        // ============================================================
        // =============== LOCALIDADES (DEVUELVE OBJETOS) ============
        // ============================================================
        public List<LocalidadDTO> ObtenerLocalidades()
        {
            return _persistencia.ObtenerLocalidades()
                .Where(l => l.nombre != "Agrícola" && l.nombre != "Móvil" && l.nombre != "Itinerante" && l.Provincia != null && !l.nombre.Contains("Desconocida"))
                .Select(l => new LocalidadDTO
                {
                   
                    NombreLocalidad = l.nombre,
                    NombreProvincia = l.Provincia.nombre
                   
                    
                })
                .OrderBy(l => l.NombreProvincia)
                .ThenBy(l => l.NombreLocalidad)
                .ToList();
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