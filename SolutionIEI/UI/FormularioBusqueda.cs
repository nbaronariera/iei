using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Microsoft.EntityFrameworkCore;
using UI.Entidades;
using UI.Parsers;
using UI.Wrappers;

namespace UI.UI_Gestor
{
    public partial class FormularioBusqueda : Form
    {
        private GMapOverlay markersOverlay;
        private GMapOverlay routeOverlay;

        private readonly LogicaUIBusqueda _logica;

        

        public FormularioBusqueda()
        {
            InitializeComponent();

            _logica = new LogicaUIBusqueda();


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

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // Tamaño mínimo de la ventana
                this.MinimumSize = new Size(800, 600);
                this.WindowState = FormWindowState.Normal;
                this.StartPosition = FormStartPosition.CenterScreen;

                // Establecer límites de tamaño DESPUÉS de que el formulario tenga medidas válidas
                splitHorizontal.Panel1MinSize = 200;
                splitHorizontal.Panel2MinSize = 300;

                // Ajustar splitter después de cargar
                splitHorizontal.SplitterDistance = 350;

                dataGridView1.AutoGenerateColumns = true;
                dataGridView1.Columns.Clear();

                PrepararCombos();
              

                // Cargar todas las estaciones al inicio
                AplicarFiltros();

                // Ajustar SplitterDistance para que no rompa al iniciar
                splitHorizontal.SplitterDistance = this.Width / 2;


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar el mapa: " + ex.Message);
            }
        }

      


        private void btnBuscar_Click(object sender, EventArgs e)
        {
            try
            {

                AplicarFiltros();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la carga: {ex.Message}");
            }
        }

        private void PrepararCombos()
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

            CargarProvincias();
            CargarLocalidades();
        }

        private void CargarProvincias()
        {
            comboProvincia.DataSource = _logica.ObtenerProvinciasParaCombo();
        }

        private void CargarLocalidades()
        {
            comboLocalidad.DataSource = _logica.ObtenerLocalidadesParaCombo();
        }

        private void comboProvincia_SelectedIndexChanged(object sender, EventArgs e)
        {
            string provincia = comboProvincia.SelectedItem?.ToString() ?? "Cualquiera";

            if (provincia == "Cualquiera")
                CargarLocalidades();
            else
                comboLocalidad.DataSource = _logica.ObtenerLocalidadesPorProvincia(provincia);
        }

        private void comboLocalidad_SelectedIndexChanged(object sender, EventArgs e)
        {
            var seleccion = comboLocalidad.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(seleccion) || seleccion == "Cualquiera") return;

            string localidad = _logica.NormalizarLocalidadCombo(seleccion);
            string provincia = _logica.ResolverProvinciaDesdeLocalidad(localidad);

            if (!string.IsNullOrEmpty(provincia) && comboProvincia.Items.Contains(provincia))
            {
                comboProvincia.SelectedItem = provincia;
            }
        }

        private void AplicarFiltros()
        {

            string cp = txtBoxCodPostal.Text.Trim();
            string prov = comboProvincia.SelectedItem?.ToString() ?? "Cualquiera";
            string loc = comboLocalidad.SelectedItem?.ToString() ?? "Cualquiera";
            string tipo = comboTipo.SelectedItem?.ToString() ?? "Cualquiera";

            var lista = _logica.BuscarEstacionesParaLista(cp, prov, loc, tipo);
            var mapa = _logica.BuscarEstacionesParaMapa(cp, prov, loc, tipo);




            ActualizarGrid(lista);
            ActualizarMapa(mapa);
        }

        private void ActualizarGrid(List<EstacionParaMostrar> estaciones)
        {

            estacionBindingSource.DataSource = estaciones;
            dataGridView1.DataSource = estacionBindingSource;



            // Usa el BindingSource del diseñador → respeta tus columnas y DisplayName
            dataGridView1.AutoGenerateColumns = true;
            estacionBindingSource.DataSource = estaciones;
            dataGridView1.DataSource = estacionBindingSource;

            dataGridView1.Columns["Localidad"].DisplayIndex = 3;
            dataGridView1.Columns["Provincia"].DisplayIndex = 4;
        }

        private void ActualizarMapa(List<EstacionParaMostrar> estaciones)
        {
            markersOverlay.Markers.Clear();

            foreach (var e in estaciones)
            {
                
                

                var punto = new PointLatLng(e.latitud, e.longitud);
                var marker = new GMarkerGoogle(punto, GMarkerGoogleType.red_dot);

                marker.ToolTipMode = MarkerTooltipMode.OnMouseOver;
                marker.ToolTipText = $"{e.nombre}";
                markersOverlay.Markers.Add(marker);
            }

            // Forzar actualización del mapa
            gMapControl1.Zoom = gMapControl1.Zoom + 0.1;
            gMapControl1.Zoom = gMapControl1.Zoom - 0.1;
        }

       



    }
}
