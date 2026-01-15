using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using UI.Entidades;
using UI.Parsers;
using UI.Parsers.ParsedObjects;
using UI.Wrappers;
using static System.Net.WebRequestMethods;

namespace UI
{
    /// <summary>
    /// Modelo para deserializar la respuesta del servidor tras una operación de persistencia.
    /// </summary>
    class RespuestaJSON
    {
        public int numRegistrosAnyadidos { get; set; }
        public String data { get; set; }
    }

    /// <summary>
    /// Formulario encargado de la gestión de datos (carga y borrado) de estaciones ITV.
    /// Actúa como cliente frente a una API REST para procesar ficheros de diferentes CC.AA.
    /// </summary>

    public partial class FormularioCarga : Form
    {
        private readonly HttpClient _http;
        public FormularioCarga()
        {
            InitializeComponent();

            // Configuración del cliente HTTP. 
            // Se utiliza un timeout infinito porque el procesamiento de archivos grandes 
            // en el servidor puede exceder los 100 segundos por defecto de HttpClient.
            _http = new HttpClient { 
                
                BaseAddress = new Uri("http://localhost:8081"),
                Timeout = Timeout.InfiniteTimeSpan

            };
        }

        /// <summary>
        /// Sincroniza el estado de todos los selectores de comunidad con el checkbox maestro.
        /// </summary>
        private void chkTodos_CheckedChanged(object sender, EventArgs e)
        {
            bool estado = chkTodos.Checked;
            chkGalicia.Checked = estado;
            chkValencia.Checked = estado;
            chkCataluna.Checked = estado;
        }


        /// <summary>
        /// Solicita al servidor el borrado completo de la base de datos tras confirmación del usuario.
        /// </summary>
        private async void btnBorrar_Click(object sender, EventArgs e)
        {
            // Verificación preventiva antes de realizar una acción destructiva
            if (MessageBox.Show("¿Seguro que quiere borrar TODOS los datos de TODAS las comunidades autónomas?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                this.Enabled = false;                    // ← Bloquea toda la ventana
                

                try
                {
                    var response = await _http.DeleteAsync("carga/delete"); // sin barra inicial
                    if (response.IsSuccessStatusCode)
                    {
                        rtbResumen.Text = " Base de datos limpiada correctamente.\n";
                        MessageBox.Show("Datos borrados correctamente.");
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        rtbResumen.Text = $" Error al borrar: {response.StatusCode}\n{error}";
                        MessageBox.Show($"Error al borrar: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al borrar: " + ex.Message);
                }
                finally
                {
                    this.Enabled = true;                     // ← Rehabilita siempre
                   
                }
            }
        }

        /// <summary>
        /// Coordina el proceso de carga secuencial de las comunidades seleccionadas.
        /// Agrega los resultados de cada petición para generar un informe final de logs.
        /// </summary>
        private async void btnCargar_Click(object sender, EventArgs e)
        {
            rtbResumen.Clear();
            StringBuilder log = new StringBuilder();
            

            this.Enabled = false;

            int totalCargadas = 0;
            List<string> todasReparadas = new List<string>();
            List<string> todasRechazadas = new List<string>();
            List<string> erroresServidor = new List<string>();

            try
            {
                // El proceso se ejecuta de forma secuencial por comunidad para no saturar 
                // el servidor y facilitar la trazabilidad de errores.

                // --- Bloque Galicia ---
                if (chkGalicia.Checked)
                {
                    var (exito, cargados, reparados, rechazados, errorMsg) = await ProcesarComunidad("gal");

                    if (exito)
                    {
                        totalCargadas += cargados;
                        if (!string.IsNullOrWhiteSpace(reparados)) todasReparadas.Add(reparados);
                        if (!string.IsNullOrWhiteSpace(rechazados)) todasRechazadas.Add(rechazados);
                    }
                    else if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        erroresServidor.Add(errorMsg);
                    }

                }

                // --- Bloque Cataluña ---
                if (chkCataluna.Checked)
                {

                    var (exito, cargados, reparados, rechazados, errorMsg) = await ProcesarComunidad("cat");
                    if (exito)
                    {
                        totalCargadas += cargados;
                        if (!string.IsNullOrWhiteSpace(reparados)) todasReparadas.Add(reparados);
                        if (!string.IsNullOrWhiteSpace(rechazados)) todasRechazadas.Add(rechazados);
                    }
                    else if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        erroresServidor.Add(errorMsg);
                    }

                }

                // --- Bloque Valencia ---

                if (chkValencia.Checked)
                {
                    var (exito, cargados, reparados, rechazados, errorMsg) = await ProcesarComunidad("cv");
                    if (exito)
                    {
                        totalCargadas += cargados;
                        if (!string.IsNullOrWhiteSpace(reparados)) todasReparadas.Add(reparados);
                        if (!string.IsNullOrWhiteSpace(rechazados)) todasRechazadas.Add(rechazados);
                    }
                    else if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        erroresServidor.Add(errorMsg);
                    }

                }

                log.AppendLine($"\nNúmero de registros cargados correctamente: {totalCargadas}");
                log.AppendLine("\nRegistros con errores y reparados:");
                log.AppendLine(todasReparadas.Count == 0 ? "(Ninguno)" : string.Join("\n", todasReparadas));
                log.AppendLine("\nRegistros con errores y rechazados:");
                log.AppendLine(todasRechazadas.Count == 0 ? "(Ninguno)" : string.Join("\n", todasRechazadas));

                if (erroresServidor.Count > 0)
                {
                    log.AppendLine("\nErrores del servidor:");
                    log.AppendLine(string.Join("\n", erroresServidor));
                }

                log.AppendLine("\n--- CARGA FINALIZADA ---");
            }
            catch (Exception ex)
            {
                log.AppendLine($"\nERROR CRÍTICO: {ex.Message}");
            }
            finally
            {
                this.Enabled = true;
            }

            rtbResumen.Text = log.ToString();
        }

        /// <summary>
        /// Realiza la llamada HTTP POST al endpoint de carga y gestiona la respuesta JSON.
        /// </summary>
        /// <param name="endpoint">Segmento final de la URL correspondiente a la comunidad autónoma.</param>
        /// <returns>Tupla con: éxito, registros cargados, detalles de reparación, detalles de rechazo y errores.</returns>
        private async Task<(bool exito, int cargados, string reparados, string rechazados, string errorMsg)> ProcesarComunidad(string endpoint)
        {
            try
            {
                var response = await _http.PostAsync($"carga/{endpoint}", null);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    var obj = JsonSerializer.Deserialize<JsonCargaResponse>(json, options);

                    return (
                        exito: true,
                        cargados: obj?.RegistrosCargados ?? 0,
                        reparados: obj?.RegistrosReparados ?? "",
                        rechazados: obj?.RegistrosRechazados ?? "",
                        errorMsg: ""
                    );
                }
                else
                {
                    // ← Aquí recuperamos exactamente lo que envía la API
                    string mensajeApi = await response.Content.ReadAsStringAsync();

                    // Si la API no devuelve nada útil, ponemos un fallback mínimo
                    if (string.IsNullOrWhiteSpace(mensajeApi))
                    {
                        mensajeApi = $"Error del servidor: {response.StatusCode} ({response.ReasonPhrase ?? "Sin descripción"})";
                    }

                    return (
                        exito: false,
                        cargados: 0,
                        reparados: "",
                        rechazados: "",
                        errorMsg: mensajeApi   // ← Mensaje directo de la API
                    );
                }
            }
            catch (Exception ex)
            {
                // Para errores de red, timeout, etc. → aquí sí usamos el mensaje de la excepción
                Debug.WriteLine($"[CLIENTE] Excepción al procesar {endpoint}: {ex.Message}");
                return (
                    exito: false,
                    cargados: 0,
                    reparados: "",
                    rechazados: "",
                    errorMsg: ex.Message
                );
            }
        }


        // Clase auxiliar para deserializar
        private class JsonCargaResponse
        {
            public int RegistrosCargados { get; set; }
            public string? RegistrosReparados { get; set; }
            public string? RegistrosRechazados { get; set; }
        }

        /// <summary>
        /// Lógica de búsqueda de archivos locales en caso de que la ruta por defecto no exista.
        /// Permite al usuario localizar manualmente los datasets.
        /// </summary>
        private string ObtenerRutaArchivo(string nombreFuente, string rutaDefecto, string filtro)
        {
            if (System.IO.File.Exists(rutaDefecto)) return rutaDefecto;

            // Si no está en la carpeta por defecto, preguntamos al usuario
            DialogResult dr = MessageBox.Show(
                $"No se encuentra el archivo de {nombreFuente} en la carpeta Fuentes.\n¿Deseas buscarlo manualmente?",
                "Archivo no encontrado", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = $"Seleccionar archivo para {nombreFuente}";
                    ofd.Filter = filtro;
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        return ofd.FileName;
                    }
                }
            }
            return null; // Cancelado
        }

        private void btnBusqueda_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK; // Indica que quiere ir a carga
            this.Close(); // Cierra búsqueda completamente
        }
    }
}