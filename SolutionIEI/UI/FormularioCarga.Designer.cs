namespace UI
{
    partial class FormularioCarga
    {
        private System.ComponentModel.IContainer components = null;

        // Controles
        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.GroupBox grpSeleccion;
        private System.Windows.Forms.CheckBox chkTodos;
        private System.Windows.Forms.CheckBox chkGalicia;
        private System.Windows.Forms.CheckBox chkValencia;
        private System.Windows.Forms.CheckBox chkCataluna;

        private System.Windows.Forms.Button btnCancelar;
        private System.Windows.Forms.Button btnCargar;
        private System.Windows.Forms.Button btnBorrar;

        private System.Windows.Forms.Label lblLog;
        private System.Windows.Forms.RichTextBox rtbResumen;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            lblTitulo = new Label();
            grpSeleccion = new GroupBox();
            chkCataluna = new CheckBox();
            chkValencia = new CheckBox();
            chkGalicia = new CheckBox();
            chkTodos = new CheckBox();
            btnCancelar = new Button();
            btnCargar = new Button();
            btnBorrar = new Button();
            lblLog = new Label();
            rtbResumen = new RichTextBox();
            grpSeleccion.SuspendLayout();
            SuspendLayout();
            // 
            // lblTitulo
            // 
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitulo.ForeColor = Color.DarkBlue;
            lblTitulo.Location = new Point(23, 27);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(371, 37);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "Carga del almacén de datos";
            // 
            // grpSeleccion
            // 
            grpSeleccion.Controls.Add(chkCataluna);
            grpSeleccion.Controls.Add(chkValencia);
            grpSeleccion.Controls.Add(chkGalicia);
            grpSeleccion.Controls.Add(chkTodos);
            grpSeleccion.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            grpSeleccion.Location = new Point(29, 93);
            grpSeleccion.Margin = new Padding(3, 4, 3, 4);
            grpSeleccion.Name = "grpSeleccion";
            grpSeleccion.Padding = new Padding(3, 4, 3, 4);
            grpSeleccion.Size = new Size(617, 133);
            grpSeleccion.TabIndex = 1;
            grpSeleccion.TabStop = false;
            grpSeleccion.Text = "Seleccione fuente:";
            // 
            // chkCataluna
            // 
            chkCataluna.AutoSize = true;
            chkCataluna.Location = new Point(206, 106);
            chkCataluna.Margin = new Padding(3, 4, 3, 4);
            chkCataluna.Name = "chkCataluna";
            chkCataluna.Size = new Size(108, 27);
            chkCataluna.TabIndex = 3;
            chkCataluna.Text = "Catalunya";
            chkCataluna.UseVisualStyleBackColor = true;
            // 
            // chkValencia
            // 
            chkValencia.AutoSize = true;
            chkValencia.Location = new Point(206, 71);
            chkValencia.Margin = new Padding(3, 4, 3, 4);
            chkValencia.Name = "chkValencia";
            chkValencia.Size = new Size(200, 27);
            chkValencia.TabIndex = 2;
            chkValencia.Text = "Comunitat Valenciana";
            chkValencia.UseVisualStyleBackColor = true;
            // 
            // chkGalicia
            // 
            chkGalicia.AutoSize = true;
            chkGalicia.Location = new Point(206, 36);
            chkGalicia.Margin = new Padding(3, 4, 3, 4);
            chkGalicia.Name = "chkGalicia";
            chkGalicia.Size = new Size(82, 27);
            chkGalicia.TabIndex = 1;
            chkGalicia.Text = "Galicia";
            chkGalicia.UseVisualStyleBackColor = true;
            // 
            // chkTodos
            // 
            chkTodos.AutoSize = true;
            chkTodos.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            chkTodos.Location = new Point(206, 0);
            chkTodos.Margin = new Padding(3, 4, 3, 4);
            chkTodos.Name = "chkTodos";
            chkTodos.Size = new Size(77, 27);
            chkTodos.TabIndex = 0;
            chkTodos.Text = "Todas";
            chkTodos.UseVisualStyleBackColor = true;
            chkTodos.CheckedChanged += chkTodos_CheckedChanged;
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(29, 253);
            btnCancelar.Margin = new Padding(3, 4, 3, 4);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(114, 47);
            btnCancelar.TabIndex = 2;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // btnCargar
            // 
            btnCargar.BackColor = Color.LightSkyBlue;
            btnCargar.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnCargar.Location = new Point(160, 253);
            btnCargar.Margin = new Padding(3, 4, 3, 4);
            btnCargar.Name = "btnCargar";
            btnCargar.Size = new Size(114, 47);
            btnCargar.TabIndex = 3;
            btnCargar.Text = "Cargar";
            btnCargar.UseVisualStyleBackColor = false;
            btnCargar.Click += btnCargar_Click;
            // 
            // btnBorrar
            // 
            btnBorrar.BackColor = Color.LightCoral;
            btnBorrar.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnBorrar.Location = new Point(291, 253);
            btnBorrar.Margin = new Padding(3, 4, 3, 4);
            btnBorrar.Name = "btnBorrar";
            btnBorrar.Size = new Size(238, 47);
            btnBorrar.TabIndex = 4;
            btnBorrar.Text = "Borrar almacén de datos";
            btnBorrar.UseVisualStyleBackColor = false;
            btnBorrar.Click += btnBorrar_Click;
            // 
            // lblLog
            // 
            lblLog.AutoSize = true;
            lblLog.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            lblLog.Location = new Point(29, 333);
            lblLog.Name = "lblLog";
            lblLog.Size = new Size(187, 23);
            lblLog.TabIndex = 5;
            lblLog.Text = "Resultado de la carga:";
            // 
            // rtbResumen
            // 
            rtbResumen.BackColor = Color.WhiteSmoke;
            rtbResumen.BorderStyle = BorderStyle.FixedSingle;
            rtbResumen.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            rtbResumen.Location = new Point(29, 373);
            rtbResumen.Margin = new Padding(3, 4, 3, 4);
            rtbResumen.Name = "rtbResumen";
            rtbResumen.ReadOnly = true;
            rtbResumen.Size = new Size(617, 265);
            rtbResumen.TabIndex = 6;
            rtbResumen.Text = "";
            // 
            // FormularioCarga
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(686, 667);
            Controls.Add(rtbResumen);
            Controls.Add(lblLog);
            Controls.Add(btnBorrar);
            Controls.Add(btnCargar);
            Controls.Add(btnCancelar);
            Controls.Add(grpSeleccion);
            Controls.Add(lblTitulo);
            Margin = new Padding(3, 4, 3, 4);
            Name = "FormularioCarga";
            Text = "Carga de Datos";
            grpSeleccion.ResumeLayout(false);
            grpSeleccion.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}