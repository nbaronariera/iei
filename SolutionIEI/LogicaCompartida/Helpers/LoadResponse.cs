using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Helpers
{
    /// <summary>
    /// La respuesta de la carga de datos
    /// </summary>
    public class LoadResponse
    {
        /// <summary>
        /// Los registros cargados exitosamente
        /// </summary>
        int RegistrosCargados;

        /// <summary>
        /// Las tablas de los registros reparados
        /// </summary>
        string RegistrosReparados;

        /// <summary>
        /// Las tablas de los registros rechazados
        /// </summary>
        string RegistrosRechazados;

        public LoadResponse(int registrosCargados, string registrosReparados, string registrosRechazados)
        {
            RegistrosCargados = registrosCargados;
            RegistrosReparados = registrosReparados;
            RegistrosRechazados = registrosRechazados;
        }
    }
}
