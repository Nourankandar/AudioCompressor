using System;
using System.Drawing;
using System.Windows.Forms;
using AudioCompressor.logic;

namespace AudioCompressor.interfaces
{
    public partial class MainForm : Form
    {
        private Button btnBrowse, btnPlay, btnCompress, btnDecompress, btnReset, btnReport, btnSave;
        private Label lblFileSize, lblDuration, lblTitle, lblWaveformTitle, lblFileName;
        private Label lblSampleRate, lblChannels, lblBitrate, lblEncoding;
        private ComboBox cbAlgorithm;
        private Panel pnlHeader, pnlProperties, pnlControls, pnlWaveform;
        
        // تعريف المدراء الجدد
        private AudioManager audioManager = new AudioManager();
        private ClickHelper clickHelper;
        private NumericUpDown numSampleRate;
        private NumericUpDown numQuantization;
        private ProgressBar progressBar;
        private Panel pnlCharts;

        public MainForm()
        {
            this.Text = "Audio Compressor Studio Pro";
            this.Size = new Size(1050, 650); 
            this.BackColor = Color.FromArgb(24, 24, 27); 
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            SetupUI();

            audioManager.InitializeUIReferences(lblFileName, lblFileSize, lblDuration, pnlWaveform, btnPlay, 
                                    lblSampleRate, lblChannels, lblBitrate, lblEncoding);

            clickHelper = new ClickHelper(audioManager);

            btnBrowse.Click += clickHelper.BtnBrowse_Click;
            btnPlay.Click += clickHelper.BtnPlay_Click;
            btnReset.Click += clickHelper.BtnReset_Click;
            btnSave.Click += clickHelper.BtnSave_Click;
            btnReport.Click += BtnReport_Click;
            btnCompress.Click += (s, e) => clickHelper.BtnCompress_Click(cbAlgorithm, numSampleRate, numQuantization, progressBar);
            btnDecompress.Click += (s, e) => clickHelper.BtnDecompress_Click(progressBar);
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
            pnlHeader = new Panel { Size = new Size(1020, 65), Location = new Point(0, 0), BackColor = Color.FromArgb(39, 39, 42) };
            lblTitle = new Label { Text = "AUDIO COMPRESSOR STUDIO PRO", Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.White, Location = new Point(25, 18), AutoSize = true };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            pnlWaveform = new Panel { Size = new Size(960, 140), Location = new Point(25, 85), BackColor = Color.FromArgb(15, 15, 17) };
            
            // هنا الرسم يذهب مباشرة لـ AudioManager
            pnlWaveform.Paint += (s, e) => audioManager.DrawWaveform(e.Graphics);
            
            lblWaveformTitle = new Label { Text = "Audio Spectrum Display", Font = new Font("Segoe UI", 9, FontStyle.Italic), ForeColor = Color.FromArgb(113, 113, 122), Location = new Point(10, 10), AutoSize = true };
            pnlWaveform.Controls.Add(lblWaveformTitle);
            this.Controls.Add(pnlWaveform);

            btnBrowse = CreateModernButton("Browse Audio File", 25, 230, 180, 45, Color.FromArgb(37, 99, 235), Color.White);
            this.Controls.Add(btnBrowse);

            btnPlay = CreateModernButton("", 215, 230, 55, 45, Color.FromArgb(22, 163, 74), Color.White);
            // رسم زر البلاي يذهب لـ AudioManager
            btnPlay.Paint += (s, e) => audioManager.DrawPlayButton(btnPlay, e.Graphics);
            this.Controls.Add(btnPlay);

            pnlProperties = new Panel { Size = new Size(330, 260), Location = new Point(25, 275), BackColor = Color.FromArgb(39, 39, 42) };
            Label lblPropHeader = new Label { Text = "File Information", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(147, 197, 253), Location = new Point(15, 12), AutoSize = true };
            lblFileName = new Label { Text = "File Name: No file selected", Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(161, 161, 170), Location = new Point(15, 45), Width = 300, AutoEllipsis = true };
            lblFileSize = new Label { Text = "File Size: -- MB", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(228, 228, 231), Location = new Point(15, 75), AutoSize = true };
            lblDuration = new Label { Text = "Duration: --:--", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(228, 228, 231), Location = new Point(15, 105), AutoSize = true };
            lblSampleRate = new Label { Text = "Sample Rate: --", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(228, 228, 231), Location = new Point(15, 135), AutoSize = true };
            lblChannels = new Label { Text = "Channels: --", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(228, 228, 231), Location = new Point(15, 165), AutoSize = true };
            lblBitrate = new Label { Text = "Bitrate: --", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(228, 228, 231), Location = new Point(15, 195), AutoSize = true };
            lblEncoding = new Label { Text = "Encoding: --", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(228, 228, 231), Location = new Point(15, 225), AutoSize = true };

            pnlProperties.Controls.AddRange(new Control[] { 
                lblPropHeader, lblFileName, lblFileSize, lblDuration, 
                lblSampleRate, lblChannels, lblBitrate, lblEncoding 
            });
            this.Controls.Add(pnlProperties);

            pnlControls = new Panel { Size = new Size(365, 300), Location = new Point(370, 260), BackColor = Color.FromArgb(39, 39, 42) };

            // 1. الإعدادات (الخوارزمية، معدل العينات، مستويات التكميم)
            Label lblSettings = new Label { Text = "Compression Settings", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(147, 197, 253), Location = new Point(15, 15), AutoSize = true };
            cbAlgorithm = new ComboBox { Location = new Point(15, 30), Width = 160 }; // اختيار الخوارزمية
            cbAlgorithm.Items.AddRange(new string[] { "DPCM", "Delta Modulation", "Nonlinear Quantization" });

            numSampleRate = new NumericUpDown { Location = new Point(185, 30), Width = 70, Minimum = 8000, Maximum = 48000, Value = 44100 };
            numQuantization = new NumericUpDown { Location = new Point(265, 30), Width = 70, Minimum = 2, Maximum = 16, Value = 8 };

            // 2. شريط التقدم (الطلب رقم 7)
            progressBar = new ProgressBar { Location = new Point(15, 70), Size = new Size(335, 20) };

            // 3. منطقة الرسوم البيانية (الطلب رقم 8)
            pnlCharts = new Panel { Location = new Point(15, 100), Size = new Size(335, 100), BackColor = Color.FromArgb(24, 24, 27) };
            Label lblChartTitle = new Label { Text = "Real-time Performance", ForeColor = Color.White, Location = new Point(5, 5) };
            pnlCharts.Controls.Add(lblChartTitle);

            // 4. أزرار التحكم
            btnCompress = CreateModernButton("Compress", 15, 220, 160, 45, Color.FromArgb(79, 70, 229), Color.White);
            btnDecompress = CreateModernButton("Decompress", 190, 220, 160, 45, Color.FromArgb(217, 119, 6), Color.White);

            pnlControls.Controls.AddRange(new Control[] { lblSettings, cbAlgorithm, numSampleRate, numQuantization, progressBar, pnlCharts, btnCompress, btnDecompress });
            this.Controls.Add(pnlControls);

            btnReset = CreateModernButton("Reset", 760, 260, 110, 45, Color.FromArgb(113, 113, 122), Color.White);
            btnReport = CreateModernButton("View Report", 880, 260, 105, 45, Color.FromArgb(9, 133, 124), Color.White);
            btnSave = CreateModernButton("Save File", 760, 340, 225, 125, Color.FromArgb(22, 163, 74), Color.White);
            btnSave.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            this.Controls.Add(btnReset); this.Controls.Add(btnReport); this.Controls.Add(btnSave);
        }

        private Button CreateModernButton(string text, int left, int top, int width, int height, Color bgColor, Color fgColor)
        {
            Button btn = new Button { Text = text, Left = left, Top = top, Width = width, Height = height, BackColor = bgColor, ForeColor = fgColor, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
        // داخل كلاس MainForm
        private void BtnReport_Click(object sender, EventArgs e)
        {
            // 1. التأكد من اختيار الخوارزمية
            if (cbAlgorithm.SelectedItem == null)
            {
                MessageBox.Show("Please select a compression algorithm first.", "Information Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. التأكد من أن المستخدم قام بالضغط أولاً لكي لا تظهر الأرقام أصفاراً
            if (audioManager.OriginalSize == 0)
            {
                MessageBox.Show("Please compress the audio file first to generate the report.", "No Compression Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selectedAlgo = cbAlgorithm.SelectedItem.ToString();
            string pureFileName = System.IO.Path.GetFileName(audioManager.SelectedFilePath);
            
            // قراءة القيم من الـ WaveFormat الحالي للملف
            int currentSampleRate = 44100; 
            int currentBitrate = 128;
            
            // 3. تمرير البيانات الحقيقية الناتجة عن عملية الضغط الحالية للتقرير 🌟
            ReportForm report = new ReportForm(
                pureFileName, 
                audioManager.OriginalSize, 
                audioManager.CompressedSize, 
                audioManager.TimeTaken, 
                selectedAlgo, 
                currentSampleRate, 
                currentBitrate
            );
            report.ShowDialog();
        }
        
    }
}