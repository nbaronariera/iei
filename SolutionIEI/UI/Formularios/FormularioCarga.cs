using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using UI.Entidades;
using UI.Parsers;
using UI.Parsers.ParsedObjects;
using UI.Wrappers;
using static System.Net.WebRequestMethods;

namespace UI
{
    public partial class FormularioCarga : Form
    {
        private readonly HttpClient _http;
        public FormularioCarga()
        {
            InitializeComponent();
            _http = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };
        }

        private void chkTodos_CheckedChanged(object sender, EventArgs e)
        {
            bool estado = chkTodos.Checked;
            chkGalicia.Checked = estado;
            chkValencia.Checked = estado;
            chkCataluna.Checked = estado;
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnBorrar_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("¿Seguro que quieres borrar TODOS los datos?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        db.Estaciones.RemoveRange(db.Estaciones);
                        db.Localidades.RemoveRange(db.Localidades);
                        db.Provincias.RemoveRange(db.Provincias);
                        db.SaveChanges();
                    }
                    rtbResumen.Text = "✅ Almacén de datos borrado correctamente.";
                    MessageBox.Show("Base de datos limpia.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al borrar: " + ex.Message);
                }
            }
        }

        private async void btnCargar_Click(object sender, EventArgs e)
        {
            rtbResumen.Clear();
            StringBuilder log = new StringBuilder();
            log.AppendLine("--- INICIO DE CARGA ---\n");

            Cursor.Current = Cursors.WaitCursor;
            bool huboCarga = false;

            string response = "";

            if (_http != null)
            {

                try
                {
                    if (chkGalicia.Checked)
                    {
                        var url = "/gal";
                        //log.AppendLine("\n--- CARGAA 1 ---");
                        response = response + "\n--- CARGA GALICIA ---\n" + await _http.PostAsync(url, null);

                        huboCarga = true;
                    }

                    if (chkCataluna.Checked)
                    {
                        //log.AppendLine("\n--- CARGA 2 ---");
                        var url = "/cat";
                        response = response + "\n--- CARGA CATALUÑA ---\n" + await _http.PostAsync(url, null);

                        huboCarga = true;
                    }

                    if (chkValencia.Checked)
                    {
                        var url = "/val";
                        //log.AppendLine("\n--- CARGA 3 ---");
                        response = response + "\n--- CARGA VALENCIA ---\n" + await _http.PostAsync(url, null);

                        huboCarga = true;
                    }

                    if (huboCarga)
                    {
                        //this.DialogResult = DialogResult.OK;
                        log.AppendLine(response);
                        log.AppendLine("\n--- CARGA FINALIZADA ---");
                    }
                    else
                    {
                        log.AppendLine("\n⚠️ No se seleccionó ninguna fuente o se canceló.");
                    }

                    rtbResumen.Text = log.ToString();
                }
                catch (Exception ex)
                {
                    rtbResumen.Text += $"\n ERROR CRÍTICO: {ex.Message}";
                    MessageBox.Show("Error durante la carga: " + ex.Message);
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }
            else {
                log.AppendLine("\n--- Error http ---");
            }
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
    }
}