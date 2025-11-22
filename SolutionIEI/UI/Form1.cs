using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;
using System.Linq;
using UI.Wrappers;
using UI.Parsers;

namespace UI.UI_Gestor
{
    public partial class Form1 : Form
    {
        private GMapOverlay markersOverlay;
        private GMapOverlay routeOverlay;

        private Dictionary<string, PointLatLng> Coordinates = new Dictionary<string, PointLatLng>()
        {
            { "Prueba", new PointLatLng(39.4702393, -0.3768049) },
            { "Prueba2", new PointLatLng(39.4762777, -0.4191981) },
            { "Prueba3", new PointLatLng(39.4038783, -0.4034584) },
            { "Prueba4", new PointLatLng(39.42429, -0.38285) }
        };

        public Form1()
        {
            InitializeComponent();


            gMapControl1.MapProvider = GMap.NET.MapProviders.OpenStreetMapProvider.Instance;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            gMapControl1.ShowCenter = false;


            markersOverlay = new GMapOverlay("markers");
            routeOverlay = new GMapOverlay("route");
            gMapControl1.Overlays.Add(markersOverlay);
            gMapControl1.Overlays.Add(routeOverlay);


            gMapControl1.MinZoom = 2;
            gMapControl1.MaxZoom = 18;
            gMapControl1.Zoom = 12;


            gMapControl1.Anchor = AnchorStyles.Top | AnchorStyles.Right;


            UpdateMapSize();


            if (Coordinates.Any())
            {
                var avgLat = Coordinates.Values.Average(p => p.Lat);
                var avgLng = Coordinates.Values.Average(p => p.Lng);
                gMapControl1.Position = new PointLatLng(avgLat, avgLng);
            }
            else
            {
                gMapControl1.Position = new PointLatLng(39.4699, -0.3763);
            }


            this.Resize += new EventHandler(Form1_Resize);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                CargarMarcadores();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar el mapa: " + ex.Message);
            }
        }

        private void CargarMarcadores()
        {
            foreach (var coordenada in Coordinates)
            {
                var marcador = new GMarkerGoogle(coordenada.Value, GMarkerGoogleType.red_small);
                marcador.ToolTipText = coordenada.Key;
                markersOverlay.Markers.Add(marcador);
            }
            gMapControl1.Refresh();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            UpdateMapSize();
        }

        private void UpdateMapSize()
        {

            int margin = 20;
            int mapWidth = (int)(this.ClientSize.Width * 0.5);
            int mapHeight = (int)(this.ClientSize.Height * 0.5);


            mapWidth = Math.Max(mapWidth, 300);
            mapHeight = Math.Max(mapHeight, 300);

            // Establecer tamaño
            gMapControl1.Size = new Size(mapWidth, mapHeight);

            // Posicionar en la esquina superior derecha
            gMapControl1.Location = new Point(this.ClientSize.Width - mapWidth - margin, margin);
        }

        private void btnCargarDatos_Click(object sender, EventArgs e)
        {
            try
            {
                string pathJsonGAL = CSVaJSONConversor.Ejecutar();
                string pathJsonCAT = XMLaJSONConversor.Ejecutar();

                var galParser = new GALParser();
                galParser.Load(pathJsonGAL);
                var datosGal = galParser.ParseList();
                galParser.FromParsedToUsefull(datosGal);
                galParser.Unload();

                var catParser = new CATParser();
                catParser.Load(pathJsonCAT);
                var datosCat = catParser.ParseList();
                catParser.FromParsedToUsefull(datosCat);
                catParser.Unload();

                MessageBox.Show("¡Carga de datos completada correctamente!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la carga: {ex.Message}");
            }
        }

    }
}