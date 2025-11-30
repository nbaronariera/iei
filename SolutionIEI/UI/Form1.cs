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
    public partial class Form1 : Form
    {
        private GMapOverlay markersOverlay;
        private GMapOverlay routeOverlay;

        private const string TIPO_FIJA = "Estacion_fija";
        private const string TIPO_MOVIL = "Estacion_movil";

        

        public Form1()
        {
            InitializeComponent();

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
                CargarProvincias();
                CargarLocalidades();

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

        private void CargarMarcadores()
        {
            markersOverlay.Markers.Clear();
            using (var db = new AppDbContext())
            {
                var estaciones = db.Estaciones
                    .Where(e => e.latitud != 0 && e.longitud != 0)
                    .ToList();

                foreach (var est in estaciones)
                {
                    var punto = new PointLatLng(est.latitud, est.longitud);
                    var marker = new GMarkerGoogle(punto, GMarkerGoogleType.red_small);
                    marker.ToolTipText = $"{est.nombre}\n{est.direccion}";
                    markersOverlay.Markers.Add(marker);
                }
            }

            gMapControl1.Refresh();
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
        }

        private void CargarProvincias()
        {
            using var db = new AppDbContext();

            var provincias = db.Provincias
                .OrderBy(p => p.nombre)
                .Select(p => p.nombre)
                .ToList();

            provincias.Insert(0, "Cualquiera");
            comboProvincia.DataSource = provincias;
        }

        private void CargarLocalidades()
        {
            using var db = new AppDbContext();

            // Traemos localidades con su provincia
            var query = db.Localidades
            .Where(l => l.nombre != "Agrícola" && l.nombre != "Móvil")
            .Select(l => new { Localidad = l.nombre, Provincia = l.Provincia != null ? l.Provincia.nombre : "" })
            .OrderBy(x => x.Provincia)
            .ThenBy(x => x.Localidad)
            .ToList();


            var lista = query
                .Select(x => $"{x.Localidad} ({x.Provincia})")
                .ToList();

            lista.Insert(0, "Cualquiera");
            comboLocalidad.DataSource = lista;
        }

        private void comboProvincia_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboProvincia.SelectedIndex <= 0)
            {
                CargarLocalidades();
                return;
            }

            string provinciaSel = comboProvincia.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(provinciaSel) || provinciaSel == "Cualquiera")
            {
                CargarLocalidades();
                return;
            }

            using var db = new AppDbContext();

            var localidades = db.Localidades
            .Where(l => l.Provincia != null && l.Provincia.nombre == provinciaSel && l.nombre != "Agrícola" && l.nombre != "Móvil")
            .OrderBy(l => l.nombre)
            .Select(l => l.nombre)
            .ToList();


            localidades.Insert(0, "Cualquiera");
            comboLocalidad.DataSource = localidades;
        }

        private void comboLocalidad_SelectedIndexChanged(object sender, EventArgs e)
        {
            var seleccion = comboLocalidad.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(seleccion) || seleccion == "Cualquiera") return;

            // Si comboLocalidad contiene "Localidad (Provincia)" (cuando vino de CargarLocalidades),
            // buscamos la parte de la provincia. Si contiene solo localidad (cuando vino de filtro por provincia),
            // hacemos una consulta para obtener su provincia.
            if (seleccion.Contains(" (") && seleccion.EndsWith(")"))
            {
                // formato "Localidad (Provincia)"
                var partes = seleccion.Split(new[] { " (" }, StringSplitOptions.None);
                var provincia = partes.Length > 1 ? partes[1].TrimEnd(')') : null;
                if (!string.IsNullOrEmpty(provincia))
                {
                    // seleccionar provincia en el combo si existe
                    if (comboProvincia.Items.Contains(provincia))
                        comboProvincia.SelectedItem = provincia;
                }
            }
            else
            {
                // solo nombre de localidad -> buscar provincia asociada
                string localidad = seleccion;
                using var db = new AppDbContext();
                var provincia = db.Localidades
                    .Where(l => l.nombre == localidad)
                    .Select(l => l.Provincia.nombre)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(provincia) && comboProvincia.Items.Contains(provincia))
                    comboProvincia.SelectedItem = provincia;
            }
        }

        private void AplicarFiltros()
        {
           

            using var db = new AppDbContext();

            var query = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .Include(db.Estaciones, (Estacion e) => e.localidad)
            .ThenInclude(l => l.Provincia)
            .AsQueryable();



            // FILTRO POR TIPO
            var tipoSeleccion = comboTipo.SelectedItem?.ToString() ?? "Cualquiera";
            if (tipoSeleccion == "Estación fija")
                query = query.Where(e => e.tipo == TipoEstacion.Estacion_fija);
            else if (tipoSeleccion == "Estación móvil")
                query = query.Where(e => e.tipo == TipoEstacion.Estacion_movil);
            else if (tipoSeleccion == "Otros")
                query = query.Where(e => e.tipo == TipoEstacion.Otros);


            // FILTRO POR CÓDIGO POSTAL
            var cp = txtBoxCodPostal.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(cp))
            {
                // Si acaban en "000" y la intención es "cualquiera", interpretas que se vacía resultados:
                // Si el usuario pone un CP que termina en "000", se dejan lista y mapa vacíos (regla tuya).
                if (cp.EndsWith("000"))
                {
                    ActualizarGrid(new List<Estacion>()); // vacía
                    ActualizarMapa(new List<Estacion>()); // vacía
                    return;
                }

                // si cp no vacío, filtrar por codigoPostal (campo en tu entidad es codigoPostal)
                query = query.Where(e => e.codigoPostal == cp);
            }

            // FILTRO POR PROVINCIA
            var provinciaSel = comboProvincia.SelectedItem?.ToString() ?? "Cualquiera";
            if (!string.IsNullOrEmpty(provinciaSel) && provinciaSel != "Cualquiera")
            {
                query = query.Where(e => e.localidad != null && e.localidad.Provincia.nombre == provinciaSel);
            }

            // FILTRO POR LOCALIDAD
            var localidadSel = comboLocalidad.SelectedItem?.ToString() ?? "Cualquiera";
            if (!string.IsNullOrEmpty(localidadSel) && localidadSel != "Cualquiera")
            {
                // Si el comboLocalidad tiene formato "Localidad (Provincia)" lo normalizamos:
                string localidadNombre = localidadSel.Contains(" (") ? localidadSel.Split(new[] { " (" }, StringSplitOptions.None)[0] : localidadSel;
                query = query.Where(e => e.localidad != null && e.localidad.nombre == localidadNombre);
            }

            var estaciones = query.ToList();

            foreach (var e in estaciones)
            {
                Debug.WriteLine($"{e.cod_estacion} localidad null? {e.localidad == null}");
            }


            ActualizarGrid(estaciones);
            ActualizarMapa(estaciones);
        }

        private void ActualizarGrid(List<Estacion> estaciones)
        {
            var datos = estaciones.Select(e => new EstacionParaMostrar
            {
                nombre = e.nombre,
                Tipo = TraducirTipo(e.tipo.ToString()),
                direccion = e.direccion,
                Provincia = e.localidad?.Provincia?.nombre ?? "",
                Localidad = e.tipo == TipoEstacion.Estacion_fija ? e.localidad?.nombre ?? "" : "",
                CP = e.tipo == TipoEstacion.Estacion_fija ? e.codigoPostal ?? "" : "",
                descripcion = e.descripcion ?? "",
                horario = e.horario ?? "",
                contacto = e.contacto ?? "",
                URL = e.URL ?? ""
            }).ToList();



            // Usa el BindingSource del diseñador → respeta tus columnas y DisplayName
            dataGridView1.AutoGenerateColumns = true;
            estacionBindingSource.DataSource = datos;
            dataGridView1.DataSource = estacionBindingSource;

            dataGridView1.Columns["Localidad"].DisplayIndex = 3;
            dataGridView1.Columns["Provincia"].DisplayIndex = 4;
        }

        private void ActualizarMapa(List<Estacion> estaciones)
        {
            markersOverlay.Markers.Clear();

            foreach (var e in estaciones)
            {
                // Solo mostrar estaciones fijas con coordenadas válidas
                if (e.tipo != TipoEstacion.Estacion_fija) continue;
                if (e.latitud == 0 || e.longitud == 0) continue;

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

        private string TraducirTipo(string tipo)
        {
            if (tipo == TIPO_FIJA) return "Estación fija";
            if (tipo == TIPO_MOVIL) return "Estación móvil";
            return "Otros";
        }



    }
}
