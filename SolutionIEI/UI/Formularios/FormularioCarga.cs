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

    class RespuestaJSON
    {
        public int numRegistrosAnyadidos { get; set; }
        public String data { get; set; }
    }

    public partial class FormularioCarga : Form
    {
        private readonly HttpClient _http;
        public FormularioCarga()
        {
            InitializeComponent();
            _http = new HttpClient { 
                
                BaseAddress = new Uri("http://localhost:8081"),
                Timeout = Timeout.InfiniteTimeSpan

            };
        }

        private void chkTodos_CheckedChanged(object sender, EventArgs e)
        {
            bool estado = chkTodos.Checked;
            chkGalicia.Checked = estado;
            chkValencia.Checked = estado;
            chkCataluna.Checked = estado;
        }

      

        private async void btnBorrar_Click(object sender, EventArgs e)
        {
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

        private async void btnCargar_Click(object sender, EventArgs e)
        {
            rtbResumen.Clear();
            StringBuilder log = new StringBuilder();
            

            this.Enabled = false;

            int totalCargadas = 0;
            List<string> todasReparadas = new List<string>();
            List<string> todasRechazadas = new List<string>();

            try
            {
                if (chkGalicia.Checked)
                {
                    var result = await CargarComunidad("gal");
                    totalCargadas += result.cargados;
                    if (!string.IsNullOrWhiteSpace(result.reparados)) todasReparadas.Add(result.reparados);
                    if (!string.IsNullOrWhiteSpace(result.rechazados)) todasRechazadas.Add(result.rechazados);
                    
                }

                if (chkCataluna.Checked)
                {
                    var result = await CargarComunidad("cat");
                    totalCargadas += result.cargados;
                    if (!string.IsNullOrWhiteSpace(result.reparados)) todasReparadas.Add(result.reparados);
                    if (!string.IsNullOrWhiteSpace(result.rechazados)) todasRechazadas.Add(result.rechazados);
                   
                }

                if (chkValencia.Checked)
                {
                    var result = await CargarComunidad("cv");
                    totalCargadas += result.cargados;
                    if (!string.IsNullOrWhiteSpace(result.reparados)) todasReparadas.Add(result.reparados);
                    if (!string.IsNullOrWhiteSpace(result.rechazados)) todasRechazadas.Add(result.rechazados);
                   
                }

                log.AppendLine($"\nNúmero de registros cargados correctamente: {totalCargadas}");
                log.AppendLine("\nRegistros con errores y reparados:");
                log.AppendLine(todasReparadas.Count == 0 ? "(Ninguno)" : string.Join("\n", todasReparadas));
                log.AppendLine("\nRegistros con errores y rechazados:");
                log.AppendLine(todasRechazadas.Count == 0 ? "(Ninguno)" : string.Join("\n", todasRechazadas));
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

        private async Task<(int cargados, string reparados, string rechazados)> CargarComunidad(string endpoint)
        {
            var response = await _http.PostAsync($"carga/{endpoint}", null);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var obj = JsonSerializer.Deserialize<JsonCargaResponse>(json, options);

                    return (
                        obj?.RegistrosCargados ?? 0,
                        obj?.RegistrosReparados ?? "",
                        obj?.RegistrosRechazados ?? ""
                    );
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CLIENTE] Error deserializando {endpoint}: {ex.Message}\nJSON: {json}");
                    return (0, "", $"Error al procesar JSON: {ex.Message}");
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return (0, "", $"Error HTTP {response.StatusCode}: {error}");
            }
        }


        // Clase auxiliar para deserializar
        private class JsonCargaResponse
        {
            public int RegistrosCargados { get; set; }
            public string? RegistrosReparados { get; set; }
            public string? RegistrosRechazados { get; set; }
        }


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