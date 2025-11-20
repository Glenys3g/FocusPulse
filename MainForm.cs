using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Button = System.Windows.Forms.Button;
using TextBox = System.Windows.Forms.TextBox;
using ToolTip = System.Windows.Forms.ToolTip;

namespace InactivityDetector
{
    public class MainForm : Form
    {
        private Label activeTimeLabel;
        private Label inactiveTimeLabel;
        private Label titleLabel;
        private Button pauseButton;
        private Button closeButton;
        private Button exportButton;
        private FocusPulse tracker;
        private Panel bloqueoPanel;
        private Label mensajeLabel;

        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;
        private TextBox margenTextBox;
        private Button aplicarMargenButton;
        private Button configurarButton;
        private System.Windows.Forms.ToolTip toolTip;

        public MainForm()
        {
            this.Text = "FocusPulse";
            this.Width = 250;
            this.Height = 230;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(30, 30, 30); // Dark theme
            this.Opacity = 0.95;
            this.TopMost = true;
            this.DoubleBuffered = true;

            UpdateCircularRegion();

            // Título
            titleLabel = new Label
            {
                Text = "✦ FocusPulse",
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 18, FontStyle.Regular),
                ForeColor = ColorTranslator.FromHtml("#00F5FF")
            };

            // Tiempo activo
            activeTimeLabel = new Label
            {
                Text = "🟢 Activo: 00:00:00.000",
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14),
                ForeColor = ColorTranslator.FromHtml("#00F5FF")
            };

            // Tiempo inactivo
            inactiveTimeLabel = new Label
            {
                Text = "💤 Inactivo: 00:00:00.000",
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14),
                ForeColor = ColorTranslator.FromHtml("#00F0B5")
            };

            // Botón de pausa
            pauseButton = new Button
            {
                Text = "⏸️",
                Size = new Size(60, 60),
                Font = new Font("Segoe UI", 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                TabStop = false
            };
            pauseButton.FlatAppearance.BorderSize = 0;
            pauseButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 50);
            pauseButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(70, 70, 70);
            pauseButton.Location = new Point(this.Width / 2 - pauseButton.Width / 2, this.Height - pauseButton.Height - 30);
            pauseButton.Click += (s, e) => tracker.TogglePause();

            // Crear el panel de bloqueo
            bloqueoPanel = new Panel();
            bloqueoPanel.Dock = DockStyle.Fill;
            bloqueoPanel.BackColor = Color.Black;
            bloqueoPanel.Visible = false; // oculto por defecto

            mensajeLabel = new Label();
            mensajeLabel.Text = "Tiempo de descanso obligatorio: 10 minutos";
            mensajeLabel.ForeColor = Color.White;
            mensajeLabel.Font = new Font("Arial", 24, FontStyle.Bold);
            mensajeLabel.Dock = DockStyle.Fill;
            mensajeLabel.TextAlign = ContentAlignment.MiddleCenter;

            closeButton = new Button
            {
                Text = "X",
                Size = new Size(40, 40),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                TabStop = false
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 50);
            closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(70, 70, 70);

            // Ubicar en la esquina superior derecha
            closeButton.Location = new Point(this.Width - closeButton.Width - 5, 5);

            // Acción al hacer clic
            closeButton.Click += (s, e) => this.Close();

            exportButton = new Button
            {
                Text = "📄",
                Size = new Size(30, 40),
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                TabStop = false
            };
            exportButton.FlatAppearance.BorderSize = 0;
            exportButton.Location = new Point(200, this.Height - 50);

            tracker = new FocusPulse();

            exportButton.Click += (s, e) =>
            {
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "PDF files (*.pdf)|*.pdf";
                    saveDialog.Title = "Guardar reporte de FocusPulse";
                    saveDialog.FileName = $"FocusPulse_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        tracker.ExportarPDF(saveDialog.FileName);
                        MessageBox.Show($"Reporte exportado en:\n{saveDialog.FileName}", "Exportación completada");
                    }
                }
            };

            margenTextBox = new TextBox
            {
                Text = "10", // valor por defecto en segundos
                Width = 60,
                Location = new Point(10, this.Height - 90),
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.Black,
                Visible = false
            };

            margenTextBox.KeyPress += (s, e) =>
            {
                // Permitir solo dígitos y teclas de control (ej. Backspace)
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true; // bloquea la tecla
                }
            };

            // Botón para aplicar margen
            aplicarMargenButton = new Button
            {
                Text = "Aplicar margen",
                Size = new Size(120, 40),
                Location = new Point(80, this.Height - 95),
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                TabStop = false,
                Visible = false // oculto por defecto
            };
            aplicarMargenButton.FlatAppearance.BorderSize = 0;
            aplicarMargenButton.Click += (s, e) =>
            {
                if (int.TryParse(margenTextBox.Text, out int segundos))
                {
                    tracker.MargenInactividad = TimeSpan.FromSeconds(segundos);
                    MessageBox.Show($"Margen de inactividad actualizado a {segundos} segundos", "Configuración aplicada");
                }
                else
                {
                    MessageBox.Show("Por favor ingresa un número válido en segundos.", "Error");
                }

                margenTextBox.Visible = !margenTextBox.Visible;
                aplicarMargenButton.Visible = !aplicarMargenButton.Visible;
            };

            configurarButton = new Button
            {
                Text = "⚙️",
                Size = new Size(30, 40),
                Location = new Point(160, this.Height - 50),
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                TabStop = false
            };
            configurarButton.FlatAppearance.BorderSize = 0;
            configurarButton.Click += (s, e) =>
            {
                // Al pulsar, mostramos/ocultamos los controles
                margenTextBox.Visible = !margenTextBox.Visible;
                aplicarMargenButton.Visible = !aplicarMargenButton.Visible;
            };

            // Agregar controles al formulario
            Controls.Add(configurarButton);
            Controls.Add(margenTextBox);
            Controls.Add(aplicarMargenButton);

            // Ajustar posición al redimensionar
            

            Controls.Add(exportButton);

            bloqueoPanel.Controls.Add(mensajeLabel);
            this.Controls.Add(bloqueoPanel);

            Controls.Add(pauseButton);
            Controls.Add(closeButton);
            Controls.Add(inactiveTimeLabel);
            Controls.Add(activeTimeLabel);
            Controls.Add(titleLabel);

            EnableDrag(this);
            EnableDrag(titleLabel);
            EnableDrag(activeTimeLabel);
            EnableDrag(inactiveTimeLabel);
            EnableDrag(pauseButton);

            tracker.OnTimeUpdated += UpdateLabels;
            tracker.OnPauseChanged += UpdatePauseButton;

            toolTip = new ToolTip();

            // Opcional: configurar estilo del tooltip
            toolTip.AutoPopDelay = 5000;   // tiempo visible (ms)
            toolTip.InitialDelay = 500;    // retardo antes de aparecer (ms)
            toolTip.ReshowDelay = 200;     // retardo entre apariciones
            toolTip.ShowAlways = true;     // se muestra incluso si el form no está activo

            // Asignar tooltips a los botones
            toolTip.SetToolTip(pauseButton, "Pausar/Reanudar el contador");
            toolTip.SetToolTip(closeButton, "Cerrar la aplicación");
            toolTip.SetToolTip(exportButton, "Exportar reporte a PDF");
            toolTip.SetToolTip(configurarButton, "Mostrar configuración de margen de inactividad");
            toolTip.SetToolTip(aplicarMargenButton, "Aplicar el margen de inactividad definido");

            //this.Resize += (s, e) =>
            //{
            //    UpdateCircularRegion();
            //    pauseButton.Location = new Point(this.Width / 2 - pauseButton.Width / 2, this.Height - pauseButton.Height - 30);
            //};

            this.Resize += (s, e) =>
            {
                UpdateCircularRegion();
                pauseButton.Location = new Point(this.Width / 2 - pauseButton.Width / 2, this.Height - pauseButton.Height - 30);
                //configurarButton.Location = new Point(10, this.Height - 140);
                margenTextBox.Location = new Point(10, this.Height - 90);
                aplicarMargenButton.Location = new Point(80, this.Height - 95);
            };

            tracker.SetupGlobalHooks();
            tracker.SetupUITimer();

            tracker.BloqueoNecesario += MostrarBloqueo;
            tracker.BloqueoFinalizado += OcultarBloqueo;
        }

        private void MostrarBloqueo()
        {
            bloqueoPanel.Visible = true;
            bloqueoPanel.BringToFront();
        }

        private void OcultarBloqueo()
        {
            bloqueoPanel.Visible = false;
        }

        private void UpdateLabels(TimeSpan active, TimeSpan inactive)
        {
            activeTimeLabel.Text = $"🟢 Activo: {active:hh\\:mm\\:ss\\.fff}";
            inactiveTimeLabel.Text = $"💤 Inactivo: {inactive:hh\\:mm\\:ss\\.fff}";
        }

        private void UpdatePauseButton(bool paused)
        {
            pauseButton.Text = paused ? "▶️" : "⏸️";
        }

        private void UpdateCircularRegion()
        {
            using var path = new GraphicsPath();
            //path.AddEllipse(0, 0, this.Width, this.Height);
            path.AddRectangle(new Rectangle(0, 0, this.Width, this.Height));
            this.Region = new Region(path);
        }

        private void EnableDrag(Control control)
        {
            control.MouseDown += MainForm_MouseDown;
            control.MouseMove += MainForm_MouseMove;
            control.MouseUp += MainForm_MouseUp;
        }

        private void MainForm_MouseDown(object? sender, MouseEventArgs e)
        {
            dragging = true;
            dragCursorPoint = Cursor.Position;
            dragFormPoint = this.Location;
        }

        private void MainForm_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!dragging) return;
            Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
            Point newLocation = Point.Add(dragFormPoint, new Size(diff));
            Rectangle screenBounds = Screen.FromControl(this).WorkingArea;
            newLocation.X = Math.Max(screenBounds.Left, Math.Min(newLocation.X, screenBounds.Right - this.Width));
            newLocation.Y = Math.Max(screenBounds.Top, Math.Min(newLocation.Y, screenBounds.Bottom - this.Height));
            this.Location = newLocation;
        }

        private void MainForm_MouseUp(object? sender, MouseEventArgs e)
        {
            dragging = false;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            tracker.Cleanup();
            base.OnFormClosed(e);
        }
    }
}
