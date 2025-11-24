using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using UI.Entidades;
using UI.Parsers;
using UI.Wrappers;

namespace UI.UI_Gestor
{
    public partial class Form1 : Form
    {
        private GMapOverlay markersOverlay;
        private GMapOverlay routeOverlay;

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
            gMapControl1.Zoom = 6;
            gMapControl1.Position = new PointLatLng(40.416775, -3.703790);

            gMapControl1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            UpdateMapSize();
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
            markersOverlay.Markers.Clear();
            using (var db = new AppDbContext())
            {
                var estaciones = db.Estaciones.Where(e => e.latitud != 0 && e.longitud != 0).ToList();
                foreach (var est in estaciones)
                {
                    var punto = new PointLatLng(est.latitud, est.longitud);
                    var marcador = new GMarkerGoogle(punto, GMarkerGoogleType.red_small);
                    marcador.ToolTipText = $"{est.nombre}\n{est.direccion}";
                    markersOverlay.Markers.Add(marcador);
                }
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
            gMapControl1.Size = new Size(mapWidth, mapHeight);
            gMapControl1.Location = new Point(this.ClientSize.Width - mapWidth - margin, margin);
        }

        private void btnCargarDatos_Click(object sender, EventArgs e)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string pathJsonGAL = CSVaJSONConversor.Ejecutar();
                string pathJsonCAT = XMLaJSONConversor.Ejecutar();
                string pathJsonCV = Path.Combine(baseDir, "Fuentes", "estaciones.json");

                var galParser = new GALExtractor();
                galParser.Load(pathJsonGAL);
                galParser.FromParsedToUsefull(galParser.ParseList());
                galParser.Unload();

                var catParser = new CATExtractor();
                catParser.Load(pathJsonCAT);
                catParser.FromParsedToUsefull(catParser.ParseList());
                catParser.Unload();

                var cvParser = new CVExtractor();
                cvParser.Load(pathJsonCV);
                cvParser.FromParsedToUsefull(cvParser.ParseList());
                cvParser.Unload();

                CargarMarcadores();
                MessageBox.Show("¡Carga de datos completada correctamente!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la carga: {ex.Message}");
            }
        }
    }
}