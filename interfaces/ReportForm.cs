
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AudioCompressor.interfaces
{
    public class ReportForm : Form
    {
        public ReportForm(
            string fileName,
            long originalSize,
            long compressedSize,
            double timeTaken,
            string algo,
            int sampleRate,
            int quantizationBits,
            int stepSize)
        {
            this.Text = "Compression Performance Report";
            this.Size = new Size(440, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(39, 39, 42);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // حساب نسبة التوفير
            double savings = 0;
            if (originalSize > 0)
                savings = ((originalSize - compressedSize) / (double)originalSize) * 100.0;

            double origKB = originalSize / 1024.0;
            double compKB = compressedSize / 1024.0;

            // تحديد أي إعدادات تؤثر فعلياً بناءً على الخوارزمية
            string settingsLine;
            switch (algo.ToUpper())
            {
                case "DPCM":
                    settingsLine =
                        $"    ▪ Used Algorithm : DPCM\n" +
                        $"    ▪ Sampling Rate  : {sampleRate} Hz\n" +
                        $"    ▪ Note           : DPCM uses fixed 8-bit differences.\n" +
                        $"                       Quantization Bits & Step Size have no effect.";
                    break;

                case "DELTA MODULATION":
                    settingsLine =
                        $"    ▪ Used Algorithm : Delta Modulation\n" +
                        $"    ▪ Sampling Rate  : {sampleRate} Hz\n" +
                        $"    ▪ Step Size      : {stepSize}  ← Active (affects quality)\n" +
                        $"    ▪ Note           : Quantization Bits has no effect.";
                    break;

                case "NONLINEAR QUANTIZATION":
                    int levels = (int)Math.Pow(2, quantizationBits);
                    settingsLine =
                        $"    ▪ Used Algorithm     : Nonlinear Quantization (A-Law)\n" +
                        $"    ▪ Sampling Rate      : {sampleRate} Hz\n" +
                        $"    ▪ Quantization Bits  : {quantizationBits} bits  ← Active\n" +
                        $"    ▪ Quantization Levels: {levels} levels\n" +
                        $"    ▪ Note               : Step Size has no effect.";
                    break;

                default:
                    settingsLine = $"    ▪ Used Algorithm : {algo}\n    ▪ Sampling Rate  : {sampleRate} Hz";
                    break;
            }

            Label lblReportText = new Label
            {
                AutoSize = true,
                Location = new Point(20, 20),
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.White
            };

            lblReportText.Text =
                $"╔══════════════════════════════════════╗\n" +
                $"   AUDIO COMPRESSION REPORT             \n" +
                $"╚══════════════════════════════════════╝\n\n" +
                $"📄 File Name: {fileName}\n\n" +

                $"📊 1. File Size:\n" +
                $"    ▪ Original Size   : {origKB:F2} KB\n" +
                $"    ▪ Compressed Size : {compKB:F2} KB\n\n" +

                $"📈 2. Space Savings:\n" +
                $"    ▪ Ratio: {savings:F2} %\n\n" +

                $"⏱️ 3. Execution Time:\n" +
                $"    ▪ Time Taken: {timeTaken:F4} Seconds\n\n" +

                $"⚙️ 4. Settings & Parameters:\n" +
                settingsLine;

            Button btnClose = new Button
            {
                Text = "Close Report",
                Size = new Size(130, 38),
                Location = new Point(145, 460),
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(lblReportText);
            this.Controls.Add(btnClose);
        }

        private void InitializeComponent() { }
    }
}