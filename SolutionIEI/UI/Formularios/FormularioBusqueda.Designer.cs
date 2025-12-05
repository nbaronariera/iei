namespace UI.UI_Gestor
{
    partial class FormularioBusqueda
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.SplitContainer splitVertical;
        private System.Windows.Forms.SplitContainer splitHorizontal;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            splitVertical = new SplitContainer();
            splitHorizontal = new SplitContainer();
            comboTipo = new ComboBox();
            comboProvincia = new ComboBox();
            comboLocalidad = new ComboBox();
            txtBoxCodPostal = new TextBox();
            lblTipo = new Label();
            lblProvincia = new Label();
            lblCodPostal = new Label();
            lblLocalidad = new Label();
            lblTitulo = new Label();
            button1 = new Button();
            btnCargarDatos = new Button();
            dataGridView1 = new DataGridView();
            lblResultados = new Label();
            colNom = new DataGridViewTextBoxColumn();
            colTipo = new DataGridViewTextBoxColumn();
            colDir = new DataGridViewTextBoxColumn();
            colCodPostal = new DataGridViewTextBoxColumn();
            colDesc = new DataGridViewTextBoxColumn();
            colHor = new DataGridViewTextBoxColumn();
            colCont = new DataGridViewTextBoxColumn();
            colURL = new DataGridViewTextBoxColumn();
            colLocalidad = new DataGridViewTextBoxColumn();
            colProvincia = new DataGridViewTextBoxColumn();
            estacionBindingSource = new BindingSource(components);
            gMapControl1 = new GMap.NET.WindowsForms.GMapControl();
            ((System.ComponentModel.ISupportInitialize)splitVertical).BeginInit();
            splitVertical.Panel1.SuspendLayout();
            splitVertical.Panel2.SuspendLayout();
            splitVertical.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitHorizontal).BeginInit();
            splitHorizontal.Panel1.SuspendLayout();
            splitHorizontal.Panel2.SuspendLayout();
            splitHorizontal.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)estacionBindingSource).BeginInit();
            SuspendLayout();
            // 
            // splitVertical
            // 
            splitVertical.Dock = DockStyle.Fill;
            splitVertical.Location = new Point(0, 0);
            splitVertical.Name = "splitVertical";
            splitVertical.Orientation = Orientation.Horizontal;
            // 
            // splitVertical.Panel1
            // 
            splitVertical.Panel1.Controls.Add(splitHorizontal);
            // 
            // splitVertical.Panel2
            // 
            splitVertical.Panel2.BackColor = Color.White;
            splitVertical.Panel2.Controls.Add(dataGridView1);
            splitVertical.Panel2.Controls.Add(lblResultados);
            splitVertical.Size = new Size(1100, 836);
            splitVertical.SplitterDistance = 593;
            splitVertical.TabIndex = 0;
            // 
            // splitHorizontal
            // 
            splitHorizontal.Dock = DockStyle.Fill;
            splitHorizontal.Location = new Point(0, 0);
            splitHorizontal.Name = "splitHorizontal";
            // 
            // splitHorizontal.Panel1
            // 
            splitHorizontal.Panel1.BackColor = Color.White;
            splitHorizontal.Panel1.Controls.Add(comboTipo);
            splitHorizontal.Panel1.Controls.Add(comboProvincia);
            splitHorizontal.Panel1.Controls.Add(comboLocalidad);
            splitHorizontal.Panel1.Controls.Add(txtBoxCodPostal);
            splitHorizontal.Panel1.Controls.Add(lblTipo);
            splitHorizontal.Panel1.Controls.Add(lblProvincia);
            splitHorizontal.Panel1.Controls.Add(lblCodPostal);
            splitHorizontal.Panel1.Controls.Add(lblLocalidad);
            splitHorizontal.Panel1.Controls.Add(lblTitulo);
            splitHorizontal.Panel1.Controls.Add(button1);
            splitHorizontal.Panel1.Controls.Add(btnCargarDatos);
            splitHorizontal.Panel1MinSize = 200;
            // 
            // splitHorizontal.Panel2
            // 
            splitHorizontal.Panel2.BackColor = Color.White;
            splitHorizontal.Panel2.Controls.Add(gMapControl1);
            splitHorizontal.Panel2MinSize = 300;
            splitHorizontal.Size = new Size(1100, 593);
            splitHorizontal.SplitterDistance = 350;
            splitHorizontal.TabIndex = 0;
            // 
            // comboTipo
            // 
            comboTipo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboTipo.FormattingEnabled = true;
            comboTipo.Location = new Point(114, 257);
            comboTipo.Name = "comboTipo";
            comboTipo.Size = new Size(200, 28);
            comboTipo.TabIndex = 10;
            // 
            // comboProvincia
            // 
            comboProvincia.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboProvincia.FormattingEnabled = true;
            comboProvincia.Location = new Point(114, 207);
            comboProvincia.Name = "comboProvincia";
            comboProvincia.Size = new Size(200, 28);
            comboProvincia.TabIndex = 9;
            comboProvincia.SelectedIndexChanged += comboProvincia_SelectedIndexChanged;
            // 
            // comboLocalidad
            // 
            comboLocalidad.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboLocalidad.FormattingEnabled = true;
            comboLocalidad.Location = new Point(114, 110);
            comboLocalidad.Name = "comboLocalidad";
            comboLocalidad.Size = new Size(200, 28);
            comboLocalidad.TabIndex = 8;
            comboLocalidad.SelectedIndexChanged += comboLocalidad_SelectedIndexChanged;
            // 
            // txtBoxCodPostal
            // 
            txtBoxCodPostal.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtBoxCodPostal.Location = new Point(114, 160);
            txtBoxCodPostal.Name = "txtBoxCodPostal";
            txtBoxCodPostal.Size = new Size(200, 27);
            txtBoxCodPostal.TabIndex = 7;
            // 
            // lblTipo
            // 
            lblTipo.AutoSize = true;
            lblTipo.Location = new Point(57, 260);
            lblTipo.Name = "lblTipo";
            lblTipo.Size = new Size(42, 20);
            lblTipo.TabIndex = 5;
            lblTipo.Text = "Tipo:";
            // 
            // lblProvincia
            // 
            lblProvincia.AutoSize = true;
            lblProvincia.Location = new Point(32, 210);
            lblProvincia.Name = "lblProvincia";
            lblProvincia.Size = new Size(72, 20);
            lblProvincia.TabIndex = 4;
            lblProvincia.Text = "Provincia:";
            // 
            // lblCodPostal
            // 
            lblCodPostal.AutoSize = true;
            lblCodPostal.Location = new Point(22, 163);
            lblCodPostal.Name = "lblCodPostal";
            lblCodPostal.Size = new Size(82, 20);
            lblCodPostal.TabIndex = 3;
            lblCodPostal.Text = "Cód Postal:";
            // 
            // lblLocalidad
            // 
            lblLocalidad.AutoSize = true;
            lblLocalidad.Location = new Point(31, 110);
            lblLocalidad.Name = "lblLocalidad";
            lblLocalidad.Size = new Size(77, 20);
            lblLocalidad.TabIndex = 2;
            lblLocalidad.Text = "Localidad:";
            // 
            // lblTitulo
            // 
            lblTitulo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point);
            lblTitulo.Location = new Point(20, 10);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(380, 41);
            lblTitulo.TabIndex = 1;
            lblTitulo.Text = "Buscador de Estaciones ITV";
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            button1.Location = new Point(96, 475);
            button1.MaximumSize = new Size(75, 23);
            button1.MinimumSize = new Size(75, 23);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 0;
            button1.Text = "Buscar";
            button1.UseVisualStyleBackColor = true;
            button1.Click += btnBuscar_Click;
            // 
            // btnCargarDatos
            // 
            btnCargarDatos.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnCargarDatos.BackColor = Color.LightGray;
            btnCargarDatos.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnCargarDatos.Location = new Point(220, 21);
            btnCargarDatos.Name = "btnCargarDatos";
            btnCargarDatos.Size = new Size(120, 30);
            btnCargarDatos.TabIndex = 11;
            btnCargarDatos.Text = "Gestionar Datos";
            btnCargarDatos.UseVisualStyleBackColor = false;
            btnCargarDatos.Click += btnCargarDatos_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(0, 41);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.RowTemplate.Height = 25;
            dataGridView1.Size = new Size(1100, 198);
            dataGridView1.TabIndex = 1;
            // 
            // lblResultados
            // 
            lblResultados.Anchor = AnchorStyles.Top;
            lblResultados.AutoSize = true;
            lblResultados.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            lblResultados.Location = new Point(454, 10);
            lblResultados.Name = "lblResultados";
            lblResultados.RightToLeft = RightToLeft.Yes;
            lblResultados.Size = new Size(244, 28);
            lblResultados.TabIndex = 0;
            lblResultados.Text = "Resultados de la búsqueda";
            // 
            // colNom
            // 
            colNom.MinimumWidth = 6;
            colNom.Name = "colNom";
            colNom.Width = 125;
            // 
            // colTipo
            // 
            colTipo.MinimumWidth = 6;
            colTipo.Name = "colTipo";
            colTipo.Width = 125;
            // 
            // colDir
            // 
            colDir.MinimumWidth = 6;
            colDir.Name = "colDir";
            colDir.Width = 125;
            // 
            // colCodPostal
            // 
            colCodPostal.MinimumWidth = 6;
            colCodPostal.Name = "colCodPostal";
            colCodPostal.Width = 125;
            // 
            // colDesc
            // 
            colDesc.MinimumWidth = 6;
            colDesc.Name = "colDesc";
            colDesc.Width = 125;
            // 
            // colHor
            // 
            colHor.MinimumWidth = 6;
            colHor.Name = "colHor";
            colHor.Width = 125;
            // 
            // colCont
            // 
            colCont.MinimumWidth = 6;
            colCont.Name = "colCont";
            colCont.Width = 125;
            // 
            // colURL
            // 
            colURL.MinimumWidth = 6;
            colURL.Name = "colURL";
            colURL.Width = 125;
            // 
            // colLocalidad
            // 
            colLocalidad.MinimumWidth = 6;
            colLocalidad.Name = "colLocalidad";
            colLocalidad.Width = 125;
            // 
            // colProvincia
            // 
            colProvincia.MinimumWidth = 6;
            colProvincia.Name = "colProvincia";
            colProvincia.Width = 125;
            // 
            // estacionBindingSource
            // 
            estacionBindingSource.DataSource = typeof(Entidades.Estacion);
            // 
            // gMapControl1
            // 
            gMapControl1.Bearing = 0F;
            gMapControl1.CanDragMap = true;
            gMapControl1.Dock = DockStyle.Fill;
            gMapControl1.EmptyTileColor = Color.Navy;
            gMapControl1.GrayScaleMode = false;
            gMapControl1.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
            gMapControl1.LevelsKeepInMemory = 5;
            gMapControl1.Location = new Point(0, 0);
            gMapControl1.MarkersEnabled = true;
            gMapControl1.MaxZoom = 2;
            gMapControl1.MinZoom = 2;
            gMapControl1.MouseWheelZoomEnabled = true;
            gMapControl1.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            gMapControl1.Name = "gMapControl1";
            gMapControl1.NegativeMode = false;
            gMapControl1.PolygonsEnabled = true;
            gMapControl1.RetryLoadTile = 0;
            gMapControl1.RoutesEnabled = true;
            gMapControl1.ScaleMode = GMap.NET.WindowsForms.ScaleModes.Integer;
            gMapControl1.SelectedAreaFillColor = Color.FromArgb(33, 65, 105, 225);
            gMapControl1.ShowTileGridLines = false;
            gMapControl1.Size = new Size(746, 593);
            gMapControl1.TabIndex = 0;
            gMapControl1.Zoom = 0D;
            gMapControl1.Load += gMapControl1_Load;
            // 
            // FormularioBusqueda
            // 
            ClientSize = new Size(1100, 836);
            Controls.Add(splitVertical);
            Name = "FormularioBusqueda";
            Text = "Buscador ITV";
            Load += Form1_Load;
            splitVertical.Panel1.ResumeLayout(false);
            splitVertical.Panel2.ResumeLayout(false);
            splitVertical.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitVertical).EndInit();
            splitVertical.ResumeLayout(false);
            splitHorizontal.Panel1.ResumeLayout(false);
            splitHorizontal.Panel1.PerformLayout();
            splitHorizontal.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitHorizontal).EndInit();
            splitHorizontal.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)estacionBindingSource).EndInit();
            ResumeLayout(false);
        }

        private Button button1;
        private Button btnCargarDatos;
        private Label lblTitulo;
        private Label lblResultados;
        private Label lblTipo;
        private Label lblProvincia;
        private Label lblCodPostal;
        private Label lblLocalidad;
        private TextBox txtBoxCodPostal;
        private ComboBox comboTipo;
        private ComboBox comboProvincia;
        private ComboBox comboLocalidad;
        private DataGridView dataGridView1;
        private BindingSource estacionBindingSource;
        private DataGridViewTextBoxColumn colNom;
        private DataGridViewTextBoxColumn colTipo;
        private DataGridViewTextBoxColumn colDir;
        private DataGridViewTextBoxColumn colCodPostal;
        private DataGridViewTextBoxColumn colDesc;
        private DataGridViewTextBoxColumn colHor;
        private DataGridViewTextBoxColumn colCont;
        private DataGridViewTextBoxColumn colURL;
        private DataGridViewTextBoxColumn colLocalidad;
        private DataGridViewTextBoxColumn colProvincia;
        private GMap.NET.WindowsForms.GMapControl gMapControl1;
    }
}