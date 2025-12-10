using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using UI.Entidades;
using UI.Parsers;
using UI.Wrappers;
using UI.Parsers.ParsedObjects;
using System.Linq;

namespace UI
{
    public partial class FormularioCarga : Form
    {
        public FormularioCarga()
        {
            InitializeComponent();
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

        private void btnCargar_Click(object sender, EventArgs e)
        {
            rtbResumen.Clear();
            StringBuilder log = new StringBuilder();
            log.AppendLine("--- INICIO DE CARGA ---\n");

            Cursor.Current = Cursors.WaitCursor;
            bool huboCarga = false;

            string defaultGal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fuentes", "Estacions_ITVEntrega.csv");
            string defaultCat = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fuentes", "ITV-CAT.xml");
            string defaultVal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fuentes", "estaciones.json");

            try
            {
                if (chkGalicia.Checked)
                {
                    string ruta = ObtenerRutaArchivo("Galicia (CSV)", defaultGal, "CSV Files|*.csv");
                    if (ruta != null)
                    {
                        log.AppendLine($"> Cargando Galicia desde: {Path.GetFileName(ruta)}");

                        string jsonPath = CSVaJSONConversor.Ejecutar(ruta);

                        var parser = new GALExtractor();

                        parser.Load(jsonPath);

                        var listaCruda = parser.ParseList();
                        var (resultados, insertados, descartados, _) = parser.FromParsedToUsefull(listaCruda);

                        parser.Unload();
                        log.AppendLine($"Insertados: {resultados.Count} (Válidos: {insertados}, Descartados: {descartados})");

                        huboCarga = true;
                    }
                    else log.AppendLine("  ⚠️ Carga Galicia cancelada (archivo no encontrado).");
                }

                if (chkCataluna.Checked)
                {
                    string ruta = ObtenerRutaArchivo("Cataluña (XML)", defaultCat, "XML Files|*.xml");
                    if (ruta != null)
                    {
                        log.AppendLine($"> Cargando Cataluña desde: {Path.GetFileName(ruta)}");
                        string jsonPath = XMLaJSONConversor.Ejecutar(ruta);

                        var parser = new CATExtractor();

                        parser.Load(jsonPath);

                        var listaCruda = parser.ParseList();
                        // CORRECCIÓN: Añadido el descarte '_' para el 4º elemento (String debug) que devuelve CATExtractor
                        var (resultados, insertados, descartados, _) = parser.FromParsedToUsefull(listaCruda);

                        parser.Unload();
                        log.AppendLine($"Insertados: {resultados.Count} (Válidos: {insertados}, Descartados: {descartados})");

                        huboCarga = true;
                    }
                    else log.AppendLine("  ⚠️ Carga Cataluña cancelada.");
                }

                if (chkValencia.Checked)
                {
                    string ruta = ObtenerRutaArchivo("Valencia (JSON)", defaultVal, "JSON Files|*.json");
                    if (ruta != null)
                    {
                        log.AppendLine($"> Cargando Valencia desde: {Path.GetFileName(ruta)}");


                        string jsonPath = CSVaJSONConversor.Ejecutar(ruta);

                        var parser = new CVExtractor();

                        parser.Load(jsonPath);

                        var listaCruda = parser.ParseList();
                        var (resultados, insertados, descartados, _) = parser.FromParsedToUsefull(listaCruda);

                        parser.Unload();
                        log.AppendLine($"Insertados: {resultados.Count} (Válidos: {insertados}, Descartados: {descartados})");

                        huboCarga = true;
                    }
                    else log.AppendLine("  ⚠️ Carga Valencia cancelada.");
                }

                if (huboCarga)
                {
                    log.AppendLine("\n--- CARGA FINALIZADA ---");
                    //this.DialogResult = DialogResult.OK;
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

        private string ObtenerRutaArchivo(string nombreFuente, string rutaDefecto, string filtro)
        {
            if (File.Exists(rutaDefecto)) return rutaDefecto;

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