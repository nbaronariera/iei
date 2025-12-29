using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;
using Microsoft.EntityFrameworkCore;
using UI.Entidades;
using UI.Formularios;
using UI.Logica;
using UI.Parsers;
using UI.Wrappers;
using LogicaCompartida.DTOs;

namespace UI.UI_Gestor
{
    public partial class FormularioBusqueda : Form
    {
        private GMapOverlay markersOverlay;
        private GMapOverlay routeOverlay;
        private readonly HttpClient _http;
        private List<ProvinciaDTO> _provinciasCompletas = new();
        private List<LocalidadDTO> _localidadesCompletas = new();

        private bool cargando = false;

        public FormularioBusqueda()
        {
            InitializeComponent();

            _http = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:8080"), // Puerto de la API de búsqueda
                Timeout = Timeout.InfiniteTimeSpan
            };

            // FIJAR EL PANEL IZQUIERDO
            splitHorizontal.FixedPanel = FixedPanel.Panel1;
            splitHorizontal.IsSplitterFixed = true;

            gMapControl1.MapProvider = GMap.NET.MapProviders.OpenStreetMapProvider.Instance;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            gMapControl1.ShowCenter = false;
            gMapControl1.Dock = DockStyle.Fill;

            markersOverlay = new GMapOverlay("markers");
            routeOverlay = new GMapOverlay("route");
            gMapControl1.Overlays.Add(markersOverlay);
            gMapControl1.Overlays.Add(routeOverlay);

            gMapControl1.MinZoom = 2;
            gMapControl1.MaxZoom = 18;
            gMapControl1.Zoom = 6;
            gMapControl1.Position = new PointLatLng(40.416775, -3.703790);

            dataGridView1.DataSource = estacionBindingSource;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;                    // Bloquear ventana mientras carga
                Cursor.Current = Cursors.WaitCursor;

                this.MinimumSize = new Size(900, 600);
                this.WindowState = FormWindowState.Normal;
                this.StartPosition = FormStartPosition.CenterScreen;

                splitHorizontal.SplitterDistance = 320;

                dataGridView1.AutoGenerateColumns = true;
                dataGridView1.Columns.Clear();

                await PrepararCombos();
                await AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar el formulario: " + ex.Message);
            }
            finally
            {
                this.Enabled = true;                     // Rehabilitar ventana
                Cursor.Current = Cursors.Default;
            }
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            _ = AplicarFiltros();
        }

        private void btnCargarDatos_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void gMapControl1_Load(object sender, EventArgs e)
        {
            // Método requerido por el diseñador
        }

        private async Task PrepararCombos()
        {
            comboProvincia.DropDownStyle = ComboBoxStyle.DropDownList;
            comboLocalidad.DropDownStyle = ComboBoxStyle.DropDownList;
            comboTipo.DropDownStyle = ComboBoxStyle.DropDownList;

            comboTipo.Items.Clear();
            comboTipo.Items.Add("Cualquiera");
            comboTipo.Items.Add("Estación fija");
            comboTipo.Items.Add("Estación móvil");
            comboTipo.Items.Add("Otros");
            comboTipo.SelectedIndex = 0;

            await CargarProvincias();
            await CargarLocalidades();
        }

        private async Task CargarProvincias()
        {
            cargando = true;
            try
            {
                var response = await _http.GetAsync("/provincias");
                Console.WriteLine($"[CLIENTE] /provincias → Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error API provincias: {response.StatusCode}");
                    MessageBox.Show($"Error al cargar provincias: {response.StatusCode}");
                    comboProvincia.DataSource = new List<string> { "Cualquiera" };
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var provincias = JsonSerializer.Deserialize<List<ProvinciaDTO>>(json, options) ?? new List<ProvinciaDTO>();

                _provinciasCompletas = provincias; // Guardar lista completa

                var nombres = provincias
                    .Select(p => p.Nombre)
                    .OrderBy(n => n)
                    .Prepend("Cualquiera")
                    .ToList();

                comboProvincia.DataSource = nombres;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENTE] Excepción al cargar provincias: {ex.Message}\n{ex.StackTrace}");
                
                comboProvincia.DataSource = new List<string> { "Cualquiera" };
            }
            finally
            {
                cargando = false;
            }
        }

        private async Task CargarLocalidades()
        {
            cargando = true;
            try
            {
                var response = await _http.GetAsync("/localidades");
                Console.WriteLine($"[CLIENTE] /localidades → Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error API localidades: {response.StatusCode}");
                    MessageBox.Show($"Error al cargar localidades: {response.StatusCode}");
                    comboLocalidad.DataSource = new List<string> { "Cualquiera" };
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var localidades = JsonSerializer.Deserialize<List<LocalidadDTO>>(json, options) ?? new List<LocalidadDTO>();

                _localidadesCompletas = localidades; // Guardar lista completa

                var nombres = localidades
                    .Select(l => $"{l.NombreLocalidad} ({l.NombreProvincia})")
                    .OrderBy(n => n)
                    .Prepend("Cualquiera")
                    .ToList();

                comboLocalidad.DataSource = nombres;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENTE] Excepción al cargar localidades: {ex.Message}\n{ex.StackTrace}");
                
                comboLocalidad.DataSource = new List<string> { "Cualquiera" };
            }
            finally
            {
                cargando = false;
            }
        }

        private void comboProvincia_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cargando) return;

            string seleccion = comboProvincia.SelectedItem?.ToString() ?? "Cualquiera";

            if (seleccion == "Cualquiera")
            {
                var nombres = _localidadesCompletas
                    .Select(l => $"{l.NombreLocalidad} ({l.NombreProvincia})")
                    .OrderBy(n => n)
                    .Prepend("Cualquiera")
                    .ToList();

                comboLocalidad.DataSource = nombres;
            }
            else
            {
                var localidadesFiltradas = _localidadesCompletas
                    .Where(l => l.NombreProvincia == seleccion)
                    .Select(l => $"{l.NombreLocalidad} ({l.NombreProvincia})")
                    .OrderBy(n => n)
                    .Prepend("Cualquiera")
                    .ToList();

                comboLocalidad.DataSource = localidadesFiltradas;
            }
        }

        private void comboLocalidad_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cargando) return;

            var seleccion = comboLocalidad.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(seleccion) || seleccion == "Cualquiera") return;

            string localidad = seleccion.Contains("(")
                ? seleccion.Substring(0, seleccion.LastIndexOf("(")).Trim()
                : seleccion;

            var prov = _localidadesCompletas
                .FirstOrDefault(l => l.NombreLocalidad == localidad)?.NombreProvincia;

            if (string.IsNullOrEmpty(prov)) return;

            cargando = true;
            try
            {
                comboProvincia.SelectedItem = prov;
            }
            finally
            {
                cargando = false;
            }
        }

        private async Task AplicarFiltros()
        {
            this.Enabled = false;                    // Bloquear ventana durante búsqueda
            Cursor.Current = Cursors.WaitCursor;

            string cp = txtBoxCodPostal.Text.Trim();
            string prov = comboProvincia.SelectedItem?.ToString() ?? "Cualquiera";
            string loc = comboLocalidad.SelectedItem?.ToString() ?? "Cualquiera";
            string tipo = comboTipo.SelectedItem?.ToString() ?? "Cualquiera";

            string provinciaParam = prov == "Cualquiera" ? "" : prov;
            string localidadParam = loc == "Cualquiera" ? ""
                : (loc.Contains("(") ? loc.Substring(0, loc.LastIndexOf("(")).Trim() : loc);
            string tipoParam = tipo == "Cualquiera" ? "" : tipo;

            var url = $"/estaciones?cp={cp}&provincia={provinciaParam}&localidad={localidadParam}&tipo={tipoParam}";
            Console.WriteLine($"[CLIENTE] Llamando a API: {url}");

            try
            {
                var response = await _http.GetAsync(url);
                Console.WriteLine($"[CLIENTE] /estaciones → Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[CLIENTE] ERROR API estaciones: {response.StatusCode} → {errorContent}");
                    MessageBox.Show($"Error al obtener estaciones: {response.StatusCode}");
                    ActualizarGrid(new List<EstacionParaMostrar>());
                    ActualizarMapa(new List<EstacionParaMostrar>());
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var resultado = JsonSerializer.Deserialize<List<EstacionParaMostrar>>(json, options) ?? new List<EstacionParaMostrar>();

                Console.WriteLine($"[CLIENTE] Estaciones recibidas: {resultado.Count}");

                var paraMapa = resultado.Where(e => e.latitud != 0 && e.longitud != 0).ToList();

                ActualizarGrid(resultado);
                ActualizarMapa(paraMapa);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENTE] Excepción en AplicarFiltros: {ex.Message}");
              
            }
            finally
            {
                this.Enabled = true;                 // Rehabilitar ventana
                Cursor.Current = Cursors.Default;
            }
        }

        private void ActualizarGrid(List<EstacionParaMostrar> estaciones)
        {
            estacionBindingSource.DataSource = estaciones;
            dataGridView1.DataSource = estacionBindingSource;

            if (dataGridView1.Columns["Localidad"] != null)
                dataGridView1.Columns["Localidad"].DisplayIndex = 3;
            if (dataGridView1.Columns["Provincia"] != null)
                dataGridView1.Columns["Provincia"].DisplayIndex = 4;

            // Convertir 0 exacto a "—" (ocurre únicamente con estaciones no fijas)
            dataGridView1.CellFormatting += (s, args) =>
            {
                if ((args.ColumnIndex == dataGridView1.Columns["latitud"].Index ||
                     args.ColumnIndex == dataGridView1.Columns["longitud"].Index) &&
                    args.Value is double d && d == 0)
                {
                    args.Value = "";
                    args.FormattingApplied = true;
                }
            };

        }

        private void ActualizarMapa(List<EstacionParaMostrar> estaciones)
        {
            markersOverlay.Markers.Clear();

            foreach (var e in estaciones)
            {
                var punto = new PointLatLng(e.latitud, e.longitud);
                var marker = new GMarkerGoogle(punto, GMarkerGoogleType.red_dot)
                {
                    ToolTipMode = MarkerTooltipMode.OnMouseOver,
                    ToolTipText = e.nombre
                };
                markersOverlay.Markers.Add(marker);
            }

            // Forzar refresco del mapa
            gMapControl1.Zoom += 0.000001;
            gMapControl1.Zoom -= 0.000001;
        }
    }
}