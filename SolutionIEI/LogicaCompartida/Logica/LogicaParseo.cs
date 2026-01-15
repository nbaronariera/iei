using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UI.Entidades;
using UI.Parsers;
using UI.Parsers.ParsedObjects;
using UI.Wrappers;

namespace UI.Logica
{
    /// <summary>
    /// Clase encargada de la lógica de orquestación para el parseo de estaciones ITV de las distintas CA.
    /// Centraliza las llamadas a los distintos conversores de formato (CSV, JSON, XML).
    /// </summary>
    public class LogicaParseo
    {
        /// <summary>
        /// Coordina la conversión de datos de estaciones de Galicia desde formato CSV a JSON.
        /// </summary>
        /// <returns>Una cadena en formato JSON con la información procesada.</returns>
        public string loadGal()
        {
            return CSVaJSONConversor.Ejecutar(); 
        }

        // <summary>
        /// Gestiona la carga y procesamiento de estaciones de la CV en formato JSON.
        /// </summary>
        /// <returns>Los datos serializados o procesados en formato JSON.</returns>
        public string loadCV()
        {
            return JSONConversor.Ejecutar();
        }

        /// <summary>
        /// Coordina la conversión de estaciones de Cataluña desde formato XML a JSON.
        /// </summary>
        /// <returns>Una cadena JSON que representa las estaciones de Cataluña.</returns>
        public string loadCat()
        {
            return XMLaJSONConversor.Ejecutar();
        }
    }
}