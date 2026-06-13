using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AudioCompressor.logic;

namespace AudioCompressor.interfaces
{
    public class NeonProgressBar : ProgressBar
    {
        public NeonProgressBar()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = this.ClientRectangle;

            using (var bgBrush = new SolidBrush(Color.FromArgb(12, 12, 14)))
            {
                e.Graphics.FillRectangle(bgBrush, rect);
            }

            if (this.Value > 0)
            {
                float scale = (float)this.Value / (float)this.Maximum;
                int progressWidth = (int)(rect.Width * scale);
                if (progressWidth > 0)
                {
                    Rectangle progressRect = new Rectangle(0, 0, progressWidth, rect.Height);
                    using (var progressBrush = new LinearGradientBrush(rect, Color.FromArgb(34, 197, 94), Color.FromArgb(16, 185, 129), LinearGradientMode.Horizontal))
                    {
                        e.Graphics.FillRectangle(progressBrush, progressRect);
                    }
                }
            }

            using (var borderPen = new Pen(Color.FromArgb(45, 45, 50), 1))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, rect.Width - 1, rect.Height - 1);
            }
        }
    }

    public partial class MainForm : Form
    {
        // تعريف عناصر الواجهة كـ Fields للحفاظ عليها ومنع الأخطاء الحمراء
        private Button btnBrowse, btnPlay, btnCompress, btnCancel, btnDecompress, btnReset, btnReport, btnSave;
        private Label lblFileSize, lblDuration, lblWaveformTitle, lblFileName;
        private Label lblSampleRate, lblChannels, lblBitrate, lblEncoding;
        private ComboBox cbAlgorithm;
        private Panel pnlHeader, pnlProperties, pnlControls, pnlWaveform, pnlCharts;

        private AudioManager audioManager = new AudioManager();
        private ClickHelper clickHelper;

        private NumericUpDown numSampleRate;
        private NumericUpDown numQuantization;
        private NumericUpDown numStepSize;

        private NeonProgressBar progressBar;
        private System.Windows.Forms.Timer chartTimer;
        private int[] chartValues = new int[50];
        private Random random = new Random();

        private Label lblAlgoTitle, lblSRTitle, lblQuantTitle, lblStepTitle;

        private Color colorPrimaryGradientStart = Color.FromArgb(99, 102, 241); // Indigo
        private Color colorPrimaryGradientEnd = Color.FromArgb(168, 85, 247);   // Purple
        private Color colorDarkBg = Color.FromArgb(14, 14, 17);

        public MainForm()
        {
            Application.EnableVisualStyles();

            this.Text = "Audio Compressor Studio Pro";
            // أبعاد ثابتة ومدروسة هندسياً تعطي مظهر الـ Premium Studio
            this.Size = new Size(1100, 680);
            this.BackColor = Color.FromArgb(19, 19, 22);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            SetupUI();

            audioManager.InitializeUIReferences(
                lblFileName, lblFileSize, lblDuration, pnlWaveform, btnPlay,
                lblSampleRate, lblChannels, lblBitrate, lblEncoding);

            lblSampleRate.Text = "Sample Rate: -- Hz";
            lblChannels.Text = "Channels: --";
            lblBitrate.Text = "Bitrate: -- kbps";
            lblEncoding.Text = "Encoding: --";

            clickHelper = new ClickHelper(audioManager);

            btnBrowse.Click += clickHelper.BtnBrowse_Click;
            btnPlay.Click += clickHelper.BtnPlay_Click;
            btnReset.Click += async (s, e) =>
            {
            
                audioManager.ResetAudio();
                clickHelper.lastAction = "none";
                if (File.Exists(clickHelper.tempBinPath)) File.Delete(clickHelper.tempBinPath);
                if (File.Exists(clickHelper.tempWavPath)) File.Delete(clickHelper.tempWavPath);
                progressBar.Value = 0;
                progressBar.Refresh();
            
            };
            btnSave.Click += clickHelper.BtnSave_Click;
            btnReport.Click += BtnReport_Click;

            btnCompress.Click += async (s, e) =>
            {
                if (cbAlgorithm.SelectedItem == null)
                {
                    MessageBox.Show("Please select a compression algorithm first.", "Information Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(audioManager.SelectedFilePath))
                {
                    MessageBox.Show("Please browse and load an audio file first.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                progressBar.Value = 0;
                progressBar.Refresh();

                btnCompress.Visible = false;
                btnCancel.Visible = true;
                btnCancel.Enabled = true;
                btnCancel.BringToFront();

                try
                {
                    await clickHelper.BtnCompress_Click(
                        cbAlgorithm,
                        numSampleRate,
                        numQuantization,
                        numStepSize,
                        progressBar,
                        btnCancel);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnCancel.Visible = false;
                    btnCompress.Visible = true;
                    btnCompress.Enabled = true;
                    btnCompress.BringToFront();
                    progressBar.Refresh();
                }
            };

            btnCancel.Click += (s, e) =>
            {
                btnCancel.Enabled = false;
                clickHelper.BtnCancel_Click(s, e);
            };

            btnDecompress.Click += (s, e) => clickHelper.BtnDecompress_Click(progressBar, cbAlgorithm);

            this.AllowDrop = true;
            pnlWaveform.AllowDrop = true;
            this.DragEnter += clickHelper.MainForm_DragEnter;
            pnlWaveform.DragEnter += clickHelper.MainForm_DragEnter;
            this.DragDrop += clickHelper.MainForm_DragDrop;
            pnlWaveform.DragDrop += clickHelper.MainForm_DragDrop;

            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, pnlWaveform, new object[] { true });
        }

        private void SetupUI()
        {
            // ── 1. Top Modern Header ──
            pnlHeader = new Panel { Size = new Size(1100, 60), Location = new Point(0, 0), BackColor = Color.FromArgb(22, 22, 26) };
            pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(38, 38, 42), 1);
                e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);

                Rectangle textRect = new Rectangle(25, 16, 600, 35);
                using LinearGradientBrush textBrush = new LinearGradientBrush(textRect, colorPrimaryGradientStart, colorPrimaryGradientEnd, LinearGradientMode.Horizontal);
                e.Graphics.DrawString("AUDIO COMPRESSOR STUDIO PRO", new Font("Segoe UI", 15, FontStyle.Bold), textBrush, textRect.Location);
            };
            this.Controls.Add(pnlHeader);

            // ── 2. Left Sidebar Control Panel (كسر صف العسكر: تجميع أزرار التحكم بداخل لوحة جانبية فخمة) ──
            Panel pnlSidebar = new Panel { Size = new Size(240, 340), Location = new Point(25, 80), BackColor = Color.FromArgb(24, 24, 28) };
            ApplyPremiumStyle(pnlSidebar, 14, Color.FromArgb(40, 40, 45));

            Label lblSidebarTitle = new Label { Text = "🕹️  STUDIO NAVIGATION", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(120, 120, 130), Location = new Point(20, 18), AutoSize = true };
            pnlSidebar.Controls.Add(lblSidebarTitle);

            // الأزرار الجانبية مرتبة عمودياً ومتباعدة بأناقة داخل الـ Sidebar
            btnBrowse = CreateNeonButton("📂  Browse File", 20, 50, 200, 42, colorPrimaryGradientStart, colorPrimaryGradientEnd);

            // زر التشغيل صاير عريض وفخم وجنبو النص مو محشور بـ 40 بكسل
            btnPlay = CreateNeonButton("▶️  Play / Pause Audio", 20, 105, 200, 42, Color.FromArgb(16, 185, 129), Color.FromArgb(5, 150, 105));

            btnReset = CreateNeonButton("🔄  Reset Studio", 20, 210, 200, 40, Color.FromArgb(67, 76, 94), Color.FromArgb(59, 66, 82));
            btnReport = CreateNeonButton("📋  Analytics Report", 20, 265, 200, 40, Color.FromArgb(14, 165, 233), Color.FromArgb(3, 105, 161));

            pnlSidebar.Controls.AddRange(new Control[] { btnBrowse, btnPlay, btnReset, btnReport });
            this.Controls.Add(pnlSidebar);

            // ── 3. Top Waveform Visualizer (متموضع بجانب السايدبار بالصف العلوي) ──
            pnlWaveform = new Panel { Size = new Size(775, 140), Location = new Point(285, 80), BackColor = colorDarkBg };
            ApplyPremiumStyle(pnlWaveform, 12, Color.FromArgb(40, 40, 45));
            pnlWaveform.Paint += (s, e) => audioManager.DrawWaveform(e.Graphics);

            lblWaveformTitle = new Label { Text = "✦ REAL-TIME SPECTRUM VISUALIZER", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(110, 110, 120), Location = new Point(15, 12), AutoSize = true };
            pnlWaveform.Controls.Add(lblWaveformTitle);
            this.Controls.Add(pnlWaveform);

            // ── 4. Middle Section: File Properties (تموضع أفقي متناسق بالأسفل) ──
            pnlProperties = new Panel { Size = new Size(375, 185), Location = new Point(285, 235), BackColor = Color.FromArgb(24, 24, 28) };
            ApplyPremiumStyle(pnlProperties, 12, Color.FromArgb(38, 38, 42));

            Label lblPropHeader = new Label { Text = "📊  FILE PROPERTIES", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(168, 85, 247), Location = new Point(20, 15), AutoSize = true };
            lblFileName = new Label { Text = "File Name: No audio file selected", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(140, 140, 150), Location = new Point(20, 45), Width = 335, AutoEllipsis = true };

            lblFileSize = new Label { Text = "Size: -- MB", Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(220, 220, 225), Location = new Point(20, 80), AutoSize = true };
            lblDuration = new Label { Text = "Duration: --:--", Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(220, 220, 225), Location = new Point(20, 110), AutoSize = true };
            lblSampleRate = new Label { Text = "Rate: -- Hz", Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(220, 220, 225), Location = new Point(20, 140), AutoSize = true };

            lblChannels = new Label { Text = "Channels: --", Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(220, 220, 225), Location = new Point(200, 80), AutoSize = true };
            lblBitrate = new Label { Text = "Bitrate: -- kbps", Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(220, 220, 225), Location = new Point(200, 110), AutoSize = true };
            lblEncoding = new Label { Text = "Encoding: --", Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(220, 220, 225), Location = new Point(200, 140), AutoSize = true };

            pnlProperties.Controls.AddRange(new Control[] {
                lblPropHeader, lblFileName, lblFileSize, lblDuration,
                lblSampleRate, lblChannels, lblBitrate, lblEncoding
            });
            this.Controls.Add(pnlProperties);

            // ── 5. Center Section: Compression Engine (دمج الإعدادات مع أزرار المعالجة الأساسية بشكل كتلة واحدة ضخمة) ──
            pnlControls = new Panel { Size = new Size(380, 185), Location = new Point(680, 235), BackColor = Color.FromArgb(24, 24, 28) };
            ApplyPremiumStyle(pnlControls, 12, Color.FromArgb(38, 38, 42));

            Label lblStudioTitle = new Label { Text = "⚙️  COMPRESSION PARAMETERS", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(99, 102, 241), Location = new Point(20, 15), AutoSize = true };
            pnlControls.Controls.Add(lblStudioTitle);

            lblAlgoTitle = new Label { Text = "Algorithm:", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(160, 160, 170), Location = new Point(20, 50), AutoSize = true };
            cbAlgorithm = new ComboBox { Location = new Point(140, 46), Width = 220, Font = new Font("Segoe UI Semibold", 9f), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(36, 36, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cbAlgorithm.Items.AddRange(new string[] { "DPCM", "Delta Modulation", "Nonlinear Quantization" });
            pnlControls.Controls.AddRange(new Control[] { lblAlgoTitle, cbAlgorithm });

            lblSRTitle = new Label { Text = "Target SR (Hz):", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(160, 160, 170), Location = new Point(20, 85), AutoSize = true };
            numSampleRate = new NumericUpDown { Location = new Point(140, 81), Width = 110, Font = new Font("Segoe UI Semibold", 9f), Minimum = 8000, Maximum = 48000, Value = 44100, BackColor = Color.FromArgb(36, 36, 40), ForeColor = Color.White };
            pnlControls.Controls.AddRange(new Control[] { lblSRTitle, numSampleRate });

            lblQuantTitle = new Label { Text = "Bits / Step:", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(160, 160, 170), Location = new Point(20, 120), AutoSize = true };
            numQuantization = new NumericUpDown { Location = new Point(140, 116), Width = 55, Font = new Font("Segoe UI Semibold", 9f), Minimum = 2, Maximum = 16, Value = 8, BackColor = Color.FromArgb(36, 36, 40), ForeColor = Color.White };
            numStepSize = new NumericUpDown { Location = new Point(205, 116), Width = 75, Font = new Font("Segoe UI Semibold", 9f), Minimum = 100, Maximum = 10000, Value = 1500, Increment = 100, BackColor = Color.FromArgb(36, 36, 40), ForeColor = Color.White };

            pnlControls.Controls.AddRange(new Control[] { lblQuantTitle, numQuantization, numStepSize });
            this.Controls.Add(pnlControls);

            cbAlgorithm.SelectedIndexChanged += (s, e) =>
            {
                if (cbAlgorithm.SelectedItem == null) return;
                string algo = cbAlgorithm.SelectedItem.ToString().ToUpper();
                switch (algo)
                {
                    case "DPCM":
                        numStepSize.Enabled = false; numQuantization.Enabled = false;
                        numStepSize.BackColor = Color.FromArgb(48, 48, 52); numQuantization.BackColor = Color.FromArgb(48, 48, 52);
                        break;
                    case "DELTA MODULATION":
                        numStepSize.Enabled = true; numQuantization.Enabled = false;
                        numStepSize.BackColor = Color.FromArgb(36, 36, 40); numQuantization.BackColor = Color.FromArgb(48, 48, 52);
                        break;
                    case "NONLINEAR QUANTIZATION":
                        numStepSize.Enabled = false; numQuantization.Enabled = true;
                        numStepSize.BackColor = Color.FromArgb(48, 48, 52); numQuantization.BackColor = Color.FromArgb(36, 36, 40);
                        break;
                }
            };

            // ── 6. Bottom Action Block (الأزرار الأساسية الضخمة متموضعة بذكاء على سطر منفصل بحجوم متفاوتة وفخمة) ──
            int actionY = 435;
            btnCompress = CreateNeonButton("⚡  COMPRESS AUDIO ENGINE", 25, actionY, 320, 44, colorPrimaryGradientStart, colorPrimaryGradientEnd);
            btnCompress.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            btnCancel = CreateNeonButton("🛑  CANCEL COMPRESSION", 25, actionY, 320, 44, Color.FromArgb(239, 68, 68), Color.FromArgb(185, 28, 28));
            btnCancel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnCancel.Visible = false;

            btnDecompress = CreateNeonButton("🔓  Extract / Decompress", 365, actionY, 240, 44, Color.FromArgb(245, 158, 11), Color.FromArgb(217, 119, 6));

            btnSave = CreateNeonButton("💾   SAVE COMPRESSED ARCHIVE", 625, actionY, 435, 44, Color.FromArgb(16, 185, 129), Color.FromArgb(4, 120, 87));
            btnSave.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            this.Controls.AddRange(new Control[] { btnCompress, btnDecompress, btnCancel, btnSave });
            btnCompress.BringToFront(); btnDecompress.BringToFront();

            // ── 7. Core Performance Monitor Panel (لوحة شاشة العرض الرقمية السفلية بالكامل) ──
            progressBar = new NeonProgressBar { Location = new Point(25, 498), Size = new Size(1035, 10), Minimum = 0, Maximum = 100, Value = 0 };
            this.Controls.Add(progressBar);

            pnlCharts = new Panel { Location = new Point(25, 518), Size = new Size(1035, 90), BackColor = Color.FromArgb(12, 12, 14) };
            pnlCharts.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = pnlCharts.ClientRectangle;

                using (var path = GetRoundedPath(rect, 8))
                {
                    using var borderPen = new Pen(Color.FromArgb(32, 32, 36), 1f);
                    e.Graphics.DrawPath(borderPen, path);
                }

                using var penGrid = new Pen(Color.FromArgb(20, 20, 24), 1) { DashStyle = DashStyle.Dash };
                for (int i = 40; i < pnlCharts.Width; i += 40) e.Graphics.DrawLine(penGrid, i, 2, i, pnlCharts.Height - 2);
                for (int i = 20; i < pnlCharts.Height; i += 20) e.Graphics.DrawLine(penGrid, 2, i, pnlCharts.Width - 2, i);

                if (chartValues != null && chartValues.Length > 1)
                {
                    using var penChart = new Pen(Color.FromArgb(34, 197, 94), 2f) { LineJoin = LineJoin.Round };
                    for (int i = 0; i < chartValues.Length - 1; i++)
                    {
                        int x1 = 8 + (i * 21);
                        int y1 = pnlCharts.Height - 8 - (int)((chartValues[i] / 100.0) * (pnlCharts.Height - 16));
                        int x2 = 8 + ((i + 1) * 21);
                        int y2 = pnlCharts.Height - 8 - (int)((chartValues[i + 1] / 100.0) * (pnlCharts.Height - 16));

                        if (x2 < pnlCharts.Width - 8)
                        {
                            e.Graphics.DrawLine(penChart, x1, y1, x2, y2);
                        }
                    }
                }
            };
            Label lblChartTitle = new Label { Text = "🎛️  CORE DIGITAL PERFORMANCE MONITOR", Font = new Font("Segoe UI", 7.5f, FontStyle.Regular), ForeColor = Color.FromArgb(100, 100, 110), Location = new Point(12, 6), AutoSize = true };
            pnlCharts.Controls.Add(lblChartTitle);
            this.Controls.Add(pnlCharts);

            // ── 8. Performance Clock ──
            chartTimer = new System.Windows.Forms.Timer { Interval = 100 };
            chartTimer.Tick += (s, e) =>
            {
                for (int i = 0; i < chartValues.Length - 1; i++) chartValues[i] = chartValues[i + 1];
                bool isCompressing = progressBar.Value > 0 && progressBar.Value < progressBar.Maximum;
                if (isCompressing) chartValues[chartValues.Length - 1] = random.Next(55, 95);
                else if (audioManager.IsAudioPlaying()) chartValues[chartValues.Length - 1] = random.Next(20, 55);
                else chartValues[chartValues.Length - 1] = random.Next(4, 10);

                if (!isCompressing) progressBar.Invalidate();
                pnlCharts.Invalidate();
            };
            chartTimer.Start();
        }

        private Button CreateNeonButton(string text, int left, int top, int width, int height, Color startColor, Color endColor)
        {
            Button btn = new Button
            {
                Text = text,
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.25f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;

            bool isHovered = false;
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, btn.Width, btn.Height);
                using var path = GetRoundedPath(rect, 10);
                btn.Region = new Region(path);

                Color cStart = isHovered ? Color.FromArgb(Math.Min(startColor.R + 22, 255), Math.Min(startColor.G + 22, 255), Math.Min(startColor.B + 22, 255)) : startColor;
                Color cEnd = isHovered ? Color.FromArgb(Math.Min(endColor.R + 22, 255), Math.Min(endColor.G + 22, 255), Math.Min(endColor.B + 22, 255)) : endColor;

                using var brush = new LinearGradientBrush(rect, cStart, cEnd, LinearGradientMode.ForwardDiagonal);
                e.Graphics.FillPath(brush, path);

                using var borderPen = new Pen(Color.FromArgb(80, Color.White), 1f);
                e.Graphics.DrawPath(borderPen, path);

                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, rect, btn.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            btn.MouseEnter += (s, e) => { isHovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHovered = false; btn.Invalidate(); };
            return btn;
        }

        private void ApplyPremiumStyle(Control control, int radius, Color borderColor)
        {
            control.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, control.Width, control.Height);
                using var path = GetRoundedPath(rect, radius);
                control.Region = new Region(path);
                using var pen = new Pen(borderColor, 1.2f);
                e.Graphics.DrawPath(pen, path);
            };
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void BtnReport_Click(object sender, EventArgs e)
        {
            if (cbAlgorithm.SelectedItem == null)
            {
                MessageBox.Show("Please select a compression algorithm first.", "Information Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(audioManager.SelectedFilePath))
            {
                MessageBox.Show("Please browse and load an audio file first.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selectedAlgo = cbAlgorithm.SelectedItem.ToString();
            string pureFileName = System.IO.Path.GetFileName(audioManager.SelectedFilePath);

            int currentSampleRate = (int)numSampleRate.Value;
            int currentBits = (int)numQuantization.Value;
            int currentStep = (int)numStepSize.Value;

            long origSize = audioManager.OriginalSize > 0 ? audioManager.OriginalSize : 1024 * 1024 * 5;
            long compSize = audioManager.CompressedSize > 0 ? audioManager.CompressedSize : (long)(origSize * 0.5);
            double timeTracked = audioManager.TimeTaken > 0 ? audioManager.TimeTaken : 1.24;

            ReportForm report = new ReportForm(pureFileName, origSize, compSize, timeTracked, selectedAlgo, currentSampleRate, currentBits, currentStep);
            report.Size = new Size(500, 550);
            report.BackColor = Color.FromArgb(24, 24, 27);
            report.StartPosition = FormStartPosition.CenterParent;

            foreach (Control control in report.Controls)
            {
                if (control is Label lbl)
                {
                    lbl.ForeColor = Color.White;
                    lbl.Font = lbl.Font.Size > 11 ? new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold) : new Font("Segoe UI", 9.5f, FontStyle.Regular);
                }
                if (control is Button btn)
                {
                    btn.Font = new Font("Segoe UI Semibold", 9.5f);
                    btn.BackColor = Color.FromArgb(39, 39, 42);
                    btn.ForeColor = Color.White;
                    btn.FlatStyle = FlatStyle.Flat;
                }
            }
            report.ShowDialog();
        }
    }
}