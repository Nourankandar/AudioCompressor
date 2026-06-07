
using System;
using System.Drawing;
using System.Windows.Forms;
using AudioCompressor.logic;

namespace AudioCompressor.interfaces
{
    public partial class MainForm : Form
    {
        // ===================================================
        //  عناصر الواجهة
        // ===================================================
        private Button btnBrowse, btnPlay, btnCompress, btnCancel,
                       btnDecompress, btnReset, btnReport, btnSave;

        private Label lblFileSize, lblDuration, lblTitle, lblWaveformTitle, lblFileName;
        private Label lblSampleRate, lblChannels, lblBitrate, lblEncoding;

        private ComboBox cbAlgorithm;

        private NumericUpDown numSampleRate;    
        private NumericUpDown numQuantization;  
        private NumericUpDown numStepSize;      

        private ProgressBar progressBar;
        private Panel pnlHeader, pnlProperties, pnlControls, pnlWaveform, pnlCharts;

        // ===================================================
        //  المدراء
        // ===================================================
        private AudioManager audioManager = new AudioManager();
        private ClickHelper clickHelper;

        // ===================================================
        //  Constructor
        // ===================================================
        public MainForm()
        {
            this.Text = "Audio Compressor Studio Pro";
            this.Size = new Size(1100, 680);
            this.BackColor = Color.FromArgb(24, 24, 27);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            SetupUI();

            audioManager.InitializeUIReferences(
                lblFileName, lblFileSize, lblDuration, pnlWaveform,
                btnPlay, lblSampleRate, lblChannels, lblBitrate, lblEncoding);

            clickHelper = new ClickHelper(audioManager);

            // ===================================================
            //  ربط الأحداث بالـ ClickHelper
            // ===================================================
            btnBrowse.Click    += clickHelper.BtnBrowse_Click;
            btnPlay.Click      += clickHelper.BtnPlay_Click;
            btnReset.Click     += clickHelper.BtnReset_Click;
            btnSave.Click      += clickHelper.BtnSave_Click;
            btnReport.Click    += BtnReport_Click;
            btnCancel.Click    += clickHelper.BtnCancel_Click;

            btnCompress.Click += (s, e) => clickHelper.BtnCompress_Click(
                cbAlgorithm,
                numSampleRate,
                numQuantization,
                numStepSize,      
                progressBar,
                btnCancel);

            // تم تمرير cbAlgorithm حتى يعرف النظام الخوارزمية لملفات bin المرفوعة خارجياً
            btnDecompress.Click += (s, e) => clickHelper.BtnDecompress_Click(progressBar, cbAlgorithm);

            this.AllowDrop = true;
            pnlWaveform.AllowDrop = true;
            this.DragEnter      += clickHelper.MainForm_DragEnter;
            pnlWaveform.DragEnter += clickHelper.MainForm_DragEnter;
            this.DragDrop       += clickHelper.MainForm_DragDrop;
            pnlWaveform.DragDrop += clickHelper.MainForm_DragDrop;

            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, pnlWaveform, new object[] { true });

            cbAlgorithm.SelectedIndexChanged += CbAlgorithm_SelectedIndexChanged;
        }

        // ===================================================
        //  تلميح ديناميكي عند اختيار الخوارزمية
        // ===================================================
        private void CbAlgorithm_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbAlgorithm.SelectedItem == null) return;
            string algo = cbAlgorithm.SelectedItem.ToString().ToUpper();

            switch (algo)
            {
                case "DPCM":
                    numStepSize.Enabled    = false; 
                    numQuantization.Enabled = false; 
                    numSampleRate.Enabled  = true;
                    numStepSize.BackColor    = Color.FromArgb(50, 50, 55);   
                    numQuantization.BackColor = Color.FromArgb(50, 50, 55);
                    numSampleRate.BackColor  = Color.FromArgb(39, 39, 42);
                    break;

                case "DELTA MODULATION":
                    numStepSize.Enabled    = true;  
                    numQuantization.Enabled = false; 
                    numSampleRate.Enabled  = true;
                    numStepSize.BackColor    = Color.FromArgb(39, 39, 42);   
                    numQuantization.BackColor = Color.FromArgb(50, 50, 55);
                    numSampleRate.BackColor  = Color.FromArgb(39, 39, 42);
                    break;

                case "NONLINEAR QUANTIZATION":
                    numStepSize.Enabled    = false; 
                    numQuantization.Enabled = true;  
                    numSampleRate.Enabled  = true;
                    numStepSize.BackColor    = Color.FromArgb(50, 50, 55);
                    numQuantization.BackColor = Color.FromArgb(39, 39, 42);   
                    numSampleRate.BackColor  = Color.FromArgb(39, 39, 42);
                    break;
            }
        }

        // ===================================================
        //  بناء الواجهة
        // ===================================================
        private void SetupUI()
        {
            // --- Header ---
            pnlHeader = new Panel
            {
                Size = new Size(1070, 65),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(39, 39, 42)
            };
            lblTitle = new Label
            {
                Text = "AUDIO COMPRESSOR STUDIO PRO",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(25, 18),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // --- Waveform Panel ---
            pnlWaveform = new Panel
            {
                Size = new Size(1010, 140),
                Location = new Point(25, 85),
                BackColor = Color.FromArgb(15, 15, 17)
            };
            pnlWaveform.Paint += (s, e) => audioManager.DrawWaveform(e.Graphics);
            lblWaveformTitle = new Label
            {
                Text = "Audio Spectrum Display",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(113, 113, 122),
                Location = new Point(10, 10),
                AutoSize = true
            };
            pnlWaveform.Controls.Add(lblWaveformTitle);
            this.Controls.Add(pnlWaveform);

            // --- Browse & Play ---
            btnBrowse = CreateModernButton("Browse Audio File", 25, 240, 180, 45,
                Color.FromArgb(37, 99, 235), Color.White);
            this.Controls.Add(btnBrowse);

            btnPlay = CreateModernButton("", 215, 240, 55, 45,
                Color.FromArgb(22, 163, 74), Color.White);
            btnPlay.Paint += (s, e) => audioManager.DrawPlayButton(btnPlay, e.Graphics);
            this.Controls.Add(btnPlay);

            // --- File Information Panel ---
            pnlProperties = new Panel
            {
                Size = new Size(330, 270),
                Location = new Point(25, 295),
                BackColor = Color.FromArgb(39, 39, 42)
            };
            Label lblPropHeader = new Label
            {
                Text = "File Information",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(147, 197, 253),
                Location = new Point(15, 12),
                AutoSize = true
            };
            lblFileName = new Label
            {
                Text = "File Name: No file selected",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(161, 161, 170),
                Location = new Point(15, 45),
                Width = 300,
                AutoEllipsis = true
            };
            lblFileSize = new Label
            {
                Text = "File Size: -- MB",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(228, 228, 231),
                Location = new Point(15, 78),
                AutoSize = true
            };
            lblDuration = new Label
            {
                Text = "Duration: --:--",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(228, 228, 231),
                Location = new Point(15, 108),
                AutoSize = true
            };
            lblSampleRate = new Label
            {
                Text = "Sample Rate: --",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(228, 228, 231),
                Location = new Point(15, 138),
                AutoSize = true
            };
            lblChannels = new Label
            {
                Text = "Channels: --",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(228, 228, 231),
                Location = new Point(15, 168),
                AutoSize = true
            };
            lblBitrate = new Label
            {
                Text = "Bitrate: --",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(228, 228, 231),
                Location = new Point(15, 198),
                AutoSize = true
            };
            lblEncoding = new Label
            {
                Text = "Encoding: --",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(228, 228, 231),
                Location = new Point(15, 228),
                AutoSize = true
            };
            pnlProperties.Controls.AddRange(new Control[]
            {
                lblPropHeader, lblFileName, lblFileSize, lblDuration,
                lblSampleRate, lblChannels, lblBitrate, lblEncoding
            });
            this.Controls.Add(pnlProperties);

            // --- Compression Settings Panel ---
            pnlControls = new Panel
            {
                Size = new Size(430, 410),
                Location = new Point(370, 285),
                BackColor = Color.FromArgb(39, 39, 42)
            };

            Label lblSettings = new Label
            {
                Text = "Compression Settings",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(147, 197, 253),
                Location = new Point(15, 12),
                AutoSize = true
            };

            Label lblAlgoHint = new Label
            {
                Text = "Algorithm:",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(161, 161, 170),
                Location = new Point(15, 38),
                AutoSize = true
            };
            cbAlgorithm = new ComboBox
            {
                Location = new Point(15, 55),
                Width = 200,
                BackColor = Color.FromArgb(39, 39, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbAlgorithm.Items.AddRange(new string[]
            {
                "DPCM",
                "Delta Modulation",
                "Nonlinear Quantization"
            });

            Label lblSRHint = new Label
            {
                Text = "Sample Rate (Hz):",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(161, 161, 170),
                Location = new Point(15, 88),
                AutoSize = true
            };
            numSampleRate = new NumericUpDown
            {
                Location = new Point(15, 105),
                Width = 120,
                Minimum = 8000,
                Maximum = 48000,
                Value = 44100,
                Increment = 100,
                BackColor = Color.FromArgb(39, 39, 42),
                ForeColor = Color.White
            };

            Label lblQuantHint = new Label
            {
                Text = "Quantization Bits (A-Law only):",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(161, 161, 170),
                Location = new Point(155, 88),
                AutoSize = true
            };
            numQuantization = new NumericUpDown
            {
                Location = new Point(155, 105),
                Width = 90,
                Minimum = 2,
                Maximum = 16,
                Value = 8,
                BackColor = Color.FromArgb(39, 39, 42),
                ForeColor = Color.White
            };

            Label lblStepHint = new Label
            {
                Text = "Step Size (Delta only):",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(161, 161, 170),
                Location = new Point(265, 88),
                AutoSize = true
            };
            numStepSize = new NumericUpDown
            {
                Location = new Point(265, 105),
                Width = 120,
                Minimum = 100,
                Maximum = 10000,
                Value = 1500,
                Increment = 100,
                BackColor = Color.FromArgb(39, 39, 42),
                ForeColor = Color.White
            };

            progressBar = new ProgressBar
            {
                Location = new Point(15, 140),
                Size = new Size(400, 20)
            };

            pnlCharts = new Panel
            {
                Location = new Point(15, 170),
                Size = new Size(400, 60),
                BackColor = Color.FromArgb(24, 24, 27)
            };
            Label lblChartTitle = new Label
            {
                Text = "Real-time Performance",
                ForeColor = Color.FromArgb(113, 113, 122),
                Location = new Point(5, 5),
                AutoSize = true
            };
            pnlCharts.Controls.Add(lblChartTitle);

            btnCompress = CreateModernButton("Compress", 15, 245, 190, 45,
                Color.FromArgb(79, 70, 229), Color.White);
            btnDecompress = CreateModernButton("Decompress", 220, 245, 190, 45,
                Color.FromArgb(217, 119, 6), Color.White);

            btnCancel = CreateModernButton("Cancel", 220, 300, 190, 45,
                Color.FromArgb(220, 38, 38), Color.White);
            btnCancel.Visible = false;

            pnlControls.Controls.AddRange(new Control[]
            {
                lblSettings,
                lblAlgoHint,  cbAlgorithm,
                lblSRHint,    numSampleRate,
                lblQuantHint, numQuantization,
                lblStepHint,  numStepSize,
                progressBar,  pnlCharts,
                btnCompress,  btnDecompress,
                btnCancel
            });
            this.Controls.Add(pnlControls);

            btnReset = CreateModernButton("Reset", 820, 285, 110, 45,
                Color.FromArgb(113, 113, 122), Color.White);
            btnReport = CreateModernButton("View Report", 940, 285, 120, 45,
                Color.FromArgb(9, 133, 124), Color.White);
            btnSave = CreateModernButton("Save File", 820, 350, 240, 130,
                Color.FromArgb(22, 163, 74), Color.White);
            btnSave.Font = new Font("Segoe UI", 12, FontStyle.Bold);

            this.Controls.Add(btnReset);
            this.Controls.Add(btnReport);
            this.Controls.Add(btnSave);
        }

        private Button CreateModernButton(string text, int left, int top,
            int width, int height, Color bgColor, Color fgColor)
        {
            Button btn = new Button
            {
                Text = text,
                Left = left, Top = top,
                Width = width, Height = height,
                BackColor = bgColor,
                ForeColor = fgColor,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void BtnReport_Click(object sender, EventArgs e)
        {
            if (cbAlgorithm.SelectedItem == null)
            {
                MessageBox.Show("Please select a compression algorithm first.",
                    "Information Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (audioManager.OriginalSize == 0)
            {
                MessageBox.Show("Please compress the audio file first to generate the report.",
                    "No Compression Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selectedAlgo  = cbAlgorithm.SelectedItem.ToString();
            string pureFileName  = System.IO.Path.GetFileName(audioManager.SelectedFilePath);
            int    currentSR     = (int)numSampleRate.Value;
            int    currentBits   = (int)numQuantization.Value;
            int    currentStep   = (int)numStepSize.Value;

            ReportForm report = new ReportForm(
                pureFileName,
                audioManager.OriginalSize,
                audioManager.CompressedSize,
                audioManager.TimeTaken,
                selectedAlgo,
                currentSR,
                currentBits,
                currentStep);

            report.ShowDialog();
        }
    }
}