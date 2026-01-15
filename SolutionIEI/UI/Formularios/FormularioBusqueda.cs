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
    /// <summary>
    /// Formulario de búsqueda y visualización de estaciones ITV en el mapa y tabla de datos
    /// Permite filtrar por provincia, localidad, código postal y tipo de estación 
    /// <\summary>

    public partial class FormularioBusqueda : Form
    {
        //Capas para el control del mapa (Gmap)
        private GMapOverlay markersOverlay;
        private GMapOverlay routeOverlay;

        private readonly HttpClient _http;

        //Caché local con provincias y localidades para evitar llamadas constantes a la API
        private List<ProvinciaDTO> _provinciasCompletas = new();
        private List<LocalidadDTO> _localidadesCompletas = new();

        private bool cargando = false; // Flag para evitar eventos disparados en cascada durante la carga
        private bool _apiVerificada = false;

        public FormularioBusqueda()
        {
            InitializeComponent();

            // Configuración del cliente HTTP apuntando al backend local
            _http = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:8080"), // Puerto de la API de búsqueda
                Timeout = TimeSpan.FromSeconds(30) // Tiempo de espera razonable
            };

            // Configuración estética y funcional del SplitContainer
            splitHorizontal.FixedPanel = FixedPanel.Panel1;
            splitHorizontal.IsSplitterFixed = true;

            // Inicialización del control de mapas (OpenStreetMap)
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

            // Posicionamiento inicial del mapa por defecto (vista global de España)
            gMapControl1.Zoom = 6;
            gMapControl1.Position = new PointLatLng(40.416775, -3.703790);

            dataGridView1.DataSource = estacionBindingSource;
        }

        /// <summary>
        /// Carga el formulario de búsqueda
        /// </summary>
        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;                    // Bloquear ventana mientras carga
                Cursor.Current = Cursors.WaitCursor;

                this.MinimumSize = new Size(1536, 864);
                this.WindowState = FormWindowState.Normal;
                this.StartPosition = FormStartPosition.CenterScreen;

                splitHorizontal.SplitterDistance = 320;

                dataGridView1.AutoGenerateColumns = true;
                dataGridView1.Columns.Clear();

                // VERIFICAR QUE LA API ESTÁ DISPONIBLE
                if (!await VerificarAPI())
                {
                    MessageBox.Show("La API de búsqueda no está disponible.\n\n" +
                                  "Asegúrate de que:\n" +
                                  "1. La API de búsqueda esté ejecutándose (puerto 8080)\n" +
                                  "2. Has cargado datos previamente en la ventana de carga",
                                  "Error de conexión",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                await PrepararCombos();
                await AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar el formulario: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Enabled = true;                     // Rehabilitar ventana
                Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// Verifica la disponibilidad del backend mediante reintentos progresivos.
        /// </summary>
        private async Task<bool> VerificarAPI()
        {
            try
            {
                // Intentar varias veces con timeout progresivo
                for (int intento = 1; intento <= 5; intento++)
                {
                    try
                    {
                        // Intento rápido de conexión
                        var cts = new System.Threading.CancellationTokenSource(); //Utilizamos el token para no bloquear el hilo de la UI
                        cts.CancelAfter(2000); // 2 segundos de timeout

                        var response = await _http.GetAsync("/provincias", cts.Token);

                        if (response.IsSuccessStatusCode)
                        {
                            _apiVerificada = true;
                            Debug.WriteLine($"[CLIENTE] API verificada correctamente en intento {intento}");
                            return true;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        Debug.WriteLine($"[CLIENTE] Timeout en intento {intento}/5");
                    }
                    catch (HttpRequestException ex)
                    {
                        Debug.WriteLine($"[CLIENTE] Error HTTP en intento {intento}/5: {ex.Message}");
                    }

                    if (intento < 5)
                    {
                        // Esperar progresivamente más tiempo
                        await Task.Delay(intento * 1000);
                    }
                }

                Debug.WriteLine("[CLIENTE] API no disponible después de 5 intentos");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CLIENTE] Error verificando API: {ex.Message}");
                return false;
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

        /// <summary>
        /// Prepara el contenido de los ComboBox
        /// </summary>
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

            // Cargar provincias con reintento
            if (!await CargarProvinciasConReintento())
            {
                // Si no se pueden cargar provincias, poner valores por defecto
                comboProvincia.DataSource = new List<string> { "Cualquiera" };
                comboLocalidad.DataSource = new List<string> { "Cualquiera" };
                return;
            }

            // Cargar localidades
            await CargarLocalidadesConReintento();
        }

        /// <summary>
        /// Intenta cargar las provincias, con tres intentos
        /// </summary>
        private async Task<bool> CargarProvinciasConReintento()
        {
            for (int intento = 1; intento <= 3; intento++)
            {
                try
                {
                    await CargarProvincias();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CLIENTE] Intento {intento} de cargar provincias falló: {ex.Message}");

                    if (intento == 3)
                    {
                        Debug.WriteLine($"[CLIENTE] No se pudieron cargar provincias después de 3 intentos");
                        return false;
                    }

                    await Task.Delay(1000 * intento);
                }
            }
            return false;
        }

        /// <summary>
        /// Intenta cargar las localidades, con tres intentos
        /// </summary>
        private async Task<bool> CargarLocalidadesConReintento()
        {
            for (int intento = 1; intento <= 3; intento++)
            {
                try
                {
                    await CargarLocalidades();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CLIENTE] Intento {intento} de cargar localidades falló: {ex.Message}");

                    if (intento == 3)
                    {
                        Debug.WriteLine($"[CLIENTE] No se pudieron cargar localidades después de 3 intentos");
                        return false;
                    }

                    await Task.Delay(1000 * intento);
                }
            }
            return false;
        }

        /// <summary>
        /// Guardamos todas las provincias en una única lista
        /// </summary>
        private async Task CargarProvincias()
        {
            cargando = true;
            try
            {
                var response = await _http.GetAsync("/provincias");
                Debug.WriteLine($"[CLIENTE] /provincias → Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[CLIENTE] Error API provincias: {response.StatusCode}");
                    throw new Exception($"Error HTTP {response.StatusCode}");
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
                Debug.WriteLine($"[CLIENTE] {provincias.Count} provincias cargadas");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CLIENTE] Excepción al cargar provincias: {ex.Message}");
                throw;
            }
            finally
            {
                cargando = false;
            }
        }

        /// <summary>
        /// Guardamos todas las localidades en una única lista
        /// </summary>
        private async Task CargarLocalidades()
        {
            cargando = true;
            try
            {
                var response = await _http.GetAsync("/localidades");
                Debug.WriteLine($"[CLIENTE] /localidades → Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[CLIENTE] Error API localidades: {response.StatusCode}");
                    throw new Exception($"Error HTTP {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var localidades = JsonSerializer.Deserialize<List<LocalidadDTO>>(json, options) ?? new List<LocalidadDTO>();

                _localidadesCompletas = localidades; // Guardar lista completa

                // === ORDENACIÓN: primero por provincia, luego por localidad ===
                var nombres = localidades
                    .OrderBy(l => l.NombreProvincia)
                    .ThenBy(l => l.NombreLocalidad)
                    .Select(l => $"{l.NombreLocalidad} ({l.NombreProvincia})")
                    .Prepend("Cualquiera")
                    .ToList();

                comboLocalidad.DataSource = nombres;
                Debug.WriteLine($"[CLIENTE] {localidades.Count} localidades cargadas");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CLIENTE] Excepción al cargar localidades: {ex.Message}");
                throw;
            }
            finally
            {
                cargando = false;
            }
        }

        /// <summary>
        /// Filtra el listado de localidades según la provincia seleccionada.
        /// </summary>
        private void comboProvincia_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cargando) return; //Si todavía se están cargando los datos no se ejecuta nada

            //Extraemos el texto de la provincia seleccionada
            string seleccion = comboProvincia.SelectedItem?.ToString() ?? "Cualquiera";

            List<string> nombres;

            if (seleccion == "Cualquiera")
            {
                // Todo ordenado por provincia y luego por localidad
                nombres = _localidadesCompletas
                    .OrderBy(l => l.NombreProvincia)
                    .ThenBy(l => l.NombreLocalidad)
                    .Select(l => $"{l.NombreLocalidad} ({l.NombreProvincia})")
                    .Prepend("Cualquiera")
                    .ToList();

                comboLocalidad.DataSource = nombres;
            }
            else
            {
                // Mostramos solo las localidades de la provincia seleccionada ordenadas alfabéticamente
                nombres = _localidadesCompletas
                    .Where(l => l.NombreProvincia == seleccion)
                    .OrderBy(l => l.NombreLocalidad)
                    .Select(l => $"{l.NombreLocalidad} ({l.NombreProvincia})")
                    .Prepend("Cualquiera")
                    .ToList();

                comboLocalidad.DataSource = nombres;
            }
        }

        /// <summary>
        /// Filtra según la localidad seleccionada
        /// </summary>
        private void comboLocalidad_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cargando) return; //Si todavía se están cargando los datos no se ejecuta nada 

            var seleccion = comboLocalidad.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(seleccion) || seleccion == "Cualquiera") return;

            // Limpiamos el nombre de la localidad eliminando la provincia entre paréntesis "Localidad (Provincia)"
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

        /// <summary>
        /// Realiza la petición a la API con los filtros seleccionados y actualiza la UI.
        /// </summary>
        private async Task AplicarFiltros()
        {
            this.Enabled = false;                    // Bloquear ventana durante búsqueda para evitar clicks concurrentes
            Cursor.Current = Cursors.WaitCursor;

            string cp = txtBoxCodPostal.Text.Trim();
            string prov = comboProvincia.SelectedItem?.ToString() ?? "";
            string loc = comboLocalidad.SelectedItem?.ToString() ?? "";
            string tipo = comboTipo.SelectedItem?.ToString() ?? "";

            // Convertir "Cualquiera" a cadena vacía para indicar a la API que queremos todas las provincias, localidades y/o tipos de estación
            string provinciaParam = prov == "Cualquiera" ? "" : prov;
            string localidadParam = loc == "Cualquiera" ? ""
                : (loc.Contains("(") ? loc.Substring(0, loc.LastIndexOf("(")).Trim() : loc);
            string tipoParam = tipo == "Cualquiera" ? "" : tipo;
            
            //Construimos la url del query
            var url = $"/estaciones?cp={cp}&provincia={provinciaParam}&localidad={localidadParam}&tipo={tipoParam}";
            Debug.WriteLine($"[CLIENTE] Llamando a API: {url}");

            try
            {
                var response = await _http.GetAsync(url);
                Debug.WriteLine($"[CLIENTE] /estaciones → Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[CLIENTE] ERROR API estaciones: {response.StatusCode} → {errorContent}");
                    MessageBox.Show($"Error al obtener estaciones: {response.StatusCode}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ActualizarGrid(new List<EstacionParaMostrar>());
                    ActualizarMapa(new List<EstacionParaMostrar>());
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var resultado = JsonSerializer.Deserialize<List<EstacionParaMostrar>>(json, options) ?? new List<EstacionParaMostrar>();

                Debug.WriteLine($"[CLIENTE] Estaciones recibidas: {resultado.Count}");

                var paraMapa = resultado.Where(e => e.latitud != 0 && e.longitud != 0).ToList();

                ActualizarGrid(resultado);
                ActualizarMapa(paraMapa);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CLIENTE] Excepción en AplicarFiltros: {ex.Message}");
                MessageBox.Show($"Error al aplicar filtros: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Enabled = true;                 // Rehabilitar ventana
                Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// Actualiza la tabla de estaciones según los parámetros seleccionados
        /// </summary>
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

        /// <summary>
        /// Dibuja los marcadores en el mapa basándose en las coordenadas de las estaciones.
        /// </summary>
        private void ActualizarMapa(List<EstacionParaMostrar> estaciones)
        {
            markersOverlay.Markers.Clear(); // Limpiar resultados anteriores

            foreach (var e in estaciones)
            {
                // Solo se posicionan estaciones con coordenadas válidas
                var punto = new PointLatLng(e.latitud, e.longitud);
                var marker = new GMarkerGoogle(punto, GMarkerGoogleType.red_dot)
                {
                    ToolTipMode = MarkerTooltipMode.OnMouseOver,
                    ToolTipText = e.nombre
                };
                markersOverlay.Markers.Add(marker);
            }

            // Forzar refresco del mapa: GMap a veces no redibuja los marcadores nuevos hasta que cambia el nivel de zoom
            gMapControl1.Zoom += 0.000001;
            gMapControl1.Zoom -= 0.000001;
        }
    }
}