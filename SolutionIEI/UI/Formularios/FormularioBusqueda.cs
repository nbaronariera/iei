using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Microsoft.EntityFrameworkCore;
using UI.Entidades;
using UI.Formularios;
using UI.Logica;
using UI.Parsers;
using UI.Wrappers;
using Newtonsoft.Json; // ← Necesario para JsonConvert

namespace UI.UI_Gestor
{
    public partial class FormularioBusqueda : Form
    {
        private GMapOverlay markersOverlay;
        private GMapOverlay routeOverlay;
        private readonly LogicaBusqueda _logica;
        private readonly HttpClient _http;
        private List<Localidad> _cacheLocalidades;

        // ← NUEVOS CAMPOS: para evitar el error "Cualquiera no es Provincia"
        private List<Provincia> _provinciasCompletas = new();
        private List<Localidad> _localidadesCompletas = new();

        private bool cargando = false;

        public FormularioBusqueda()
        {
            InitializeComponent();
            _http = new HttpClient { BaseAddress = new Uri("http://localhost:5001") };

            // Configuración inicial del mapa
            gMapControl1.MapProvider = GMap.NET.MapProviders.OpenStreetMapProvider.Instance;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            gMapControl1.ShowCenter = false;
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
                this.MinimumSize = new Size(800, 600);
                this.WindowState = FormWindowState.Normal;
                this.StartPosition = FormStartPosition.CenterScreen;

                splitHorizontal.Panel1MinSize = 200;
                splitHorizontal.Panel2MinSize = 300;
                splitHorizontal.SplitterDistance = 350;

                dataGridView1.AutoGenerateColumns = true;
                dataGridView1.Columns.Clear();

                await PrepararCombos();
                await AplicarFiltros();

                splitHorizontal.SplitterDistance = this.Width / 2;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar el formulario: " + ex.Message);
            }
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            _ = AplicarFiltros();
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
            cargando = true; // ← evita SelectedIndexChanged prematuro

            try
            {
                var response = await _http.GetAsync("/provincias");
                Debug.WriteLine($"[CLIENTE] /provincias → Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Error API provincias: {response.StatusCode}");
                    MessageBox.Show($"Error API provincias: {response.StatusCode}");
                    return;
                }

               
                var json = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[CLIENTE] JSON provincias ({json.Length} caracteres): {json.Substring(0, Math.Min(500, json.Length))}...");

                var provincias = JsonConvert.DeserializeObject<List<Provincia>>(json) ?? new List<Provincia>();
                Debug.WriteLine($"[CLIENTE] Provincias deserializadas: {provincias.Count}");

                _provinciasCompletas = provincias;

                var nombres = provincias.Select(p => p.nombre).Prepend("Cualquiera").ToList();
                comboProvincia.DataSource = nombres;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando provincias: " + ex.Message);
                Debug.WriteLine($"[CLIENTE] Excepción al cargar provincias: {ex.Message}\n{ex.StackTrace}");
                comboProvincia.DataSource = new List<string> { "Cualquiera" };
            }
            cargando = false; // ← ahora sí se permitirá detectar selección real
        }

        private async Task CargarLocalidades()
        {

            cargando = true;

            try
            {
                var response = await _http.GetAsync("/localidades");
                Debug.WriteLine($"[CLIENTE] /localidades → Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[CLIENTE] ERROR API provincias: {response.StatusCode}");
                    MessageBox.Show($"Error API localidades: {response.StatusCode}");
                    comboProvincia.DataSource = new List<string> { "Cualquiera" };
                    return;
                }


                

                var json = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[CLIENTE] JSON localidades ({json.Length} caracteres): {(json.Length > 500 ? json.Substring(0, 500) + "..." : json)}");

                var localidades = JsonConvert.DeserializeObject<List<Localidad>>(json) ?? new List<Localidad>();
                Debug.WriteLine($"[CLIENTE] Localidades deserializadas: {localidades.Count} elementos");

                _localidadesCompletas = localidades;
                _cacheLocalidades = localidades; // sigue funcionando como antes

                // Carga todas las localidades al inicio
                var nombres = localidades
                    .Select(l => $"{l.nombre} ({l.Provincia?.nombre ?? "Desconocida"})")
                    .Prepend("Cualquiera")
                    .ToList();

                comboLocalidad.DataSource = nombres;
                Debug.WriteLine($"[CLIENTE] comboLocalidad rellenado con {nombres.Count} elementos (incluye 'Cualquiera')");
            
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CLIENTE] EXCEPCIÓN en CargarLocalidades: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show("Error cargando localidades: " + ex.Message);
            }

            cargando = false;
        }

        private void comboProvincia_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cargando) return;

            string seleccion = comboProvincia.SelectedItem?.ToString() ?? "Cualquiera";

            if (seleccion == "Cualquiera")
            {
                // Mostrar todas las localidades
                var nombres = _localidadesCompletas
                    .Select(l => $"{l.nombre} ({l.Provincia?.nombre ?? "Desconocida"})")
                    .Prepend("Cualquiera")
                    .ToList();
                comboLocalidad.DataSource = nombres;
            }
            else
            {
                // Filtrar por provincia seleccionada
                var localidadesFiltradas = _localidadesCompletas
                    .Where(l => l.Provincia?.nombre == seleccion)
                    .Select(l => $"{l.nombre} ({l.Provincia?.nombre})")
                    .Prepend("Cualquiera")
                    .ToList();

                comboLocalidad.DataSource = localidadesFiltradas;
            }
        }

        private void comboLocalidad_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Si estamos en modo carga o no hay selección válida, salir.
            if (cargando) return;

            var seleccion = comboLocalidad.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(seleccion) || seleccion == "Cualquiera") return;

            // Normaliza: quita " (Provincia)" si está presente
            string localidad = UtilidadesFormularioBusqueda.NormalizarLocalidadCombo(seleccion);

            // Resuelve la provincia usando la cache del cliente (lista completa)
            string provincia = UtilidadesFormularioBusqueda.ResolverProvinciaDesdeLocalidad(localidad, _cacheLocalidades);

            if (string.IsNullOrEmpty(provincia)) return;

            try
            {
                // Evitar que el SelectedIndexChanged del comboProvincia reaccione durante la asignación.
                cargando = true;
                comboProvincia.BeginUpdate();

                // Buscamos la cadena exactamente (sin trim/extra spaces)
                int idx = comboProvincia.FindStringExact(provincia);

                Debug.WriteLine($"[DEBUG] Intentando seleccionar provincia '{provincia}' - FindStringExact -> idx={idx}");

                if (idx >= 0)
                {
                    comboProvincia.SelectedIndex = idx;
                }
                else
                {
                    // Intento de fallback: comparar por Trim + IgnoreCase
                    var items = comboProvincia.Items.Cast<object>()
                                   .Select(x => x?.ToString()?.Trim())
                                   .ToList();

                    int idx2 = items.FindIndex(s => string.Equals(s, provincia.Trim(), StringComparison.OrdinalIgnoreCase));
                    Debug.WriteLine($"[DEBUG] Fallback FindIndex (Trim+IgnoreCase) -> idx2={idx2}");
                    if (idx2 >= 0)
                        comboProvincia.SelectedIndex = idx2;
                    else
                    {
                        // Log para diagnosticar porqué no lo encontró (útil en desarrollo)
                        Debug.WriteLine($"[DEBUG] Provincia '{provincia}' NO encontrada entre items: {string.Join(", ", items.Take(20))}...");
                        // No hacemos nada más (dejará "Cualquiera")
                    }
                }
            }
            finally
            {
                comboProvincia.EndUpdate();
                cargando = false;
                // Forzar repintado visual por si acaso
                comboProvincia.Refresh();
            }
        }


        private async Task AplicarFiltros()
        {
            string cp = txtBoxCodPostal.Text.Trim();

            string prov = comboProvincia.SelectedItem?.ToString() ?? "Cualquiera";
            string loc = comboLocalidad.SelectedItem?.ToString() ?? "Cualquiera";
            string tipo = comboTipo.SelectedItem?.ToString() ?? "Cualquiera";

            // ← CLAVE: convertir "Cualquiera" a cadena vacía para que la API lo entienda
            string provinciaParam = prov == "Cualquiera" ? "" : prov;
            string localidadParam = loc == "Cualquiera" || loc.Contains("(")
                ? UtilidadesFormularioBusqueda.NormalizarLocalidadCombo(loc)  // quita " (Madrid)"
                : loc;
            localidadParam = localidadParam == "Cualquiera" ? "" : localidadParam;

            string tipoParam = tipo == "Cualquiera" ? "" : tipo;

            var url = $"/estaciones?cp={cp}&provincia={provinciaParam}&localidad={localidadParam}&tipo={tipoParam}";
            Debug.WriteLine($"[CLIENTE] Llamando a API corregida: {url}");

            try
            {
                var response = await _http.GetAsync(url);
                Debug.WriteLine($"[CLIENTE] /estaciones → Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[CLIENTE] ERROR API estaciones: {response.StatusCode} → {await response.Content.ReadAsStringAsync()}");
                    MessageBox.Show($"Error API estaciones: {response.StatusCode}");
                    ActualizarGrid(new List<EstacionParaMostrar>());
                    ActualizarMapa(new List<EstacionParaMostrar>());
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var resultado = JsonConvert.DeserializeObject<List<EstacionParaMostrar>>(json) ?? new List<EstacionParaMostrar>();
                Debug.WriteLine($"[CLIENTE] Estaciones recibidas: {resultado.Count}");

                var paraMapa = UtilidadesFormularioBusqueda.FiltrarParaMapa(resultado);
                ActualizarGrid(resultado);
                ActualizarMapa(paraMapa);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CLIENTE] Excepción en AplicarFiltros: {ex.Message}");
                MessageBox.Show("Error: " + ex.Message);
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