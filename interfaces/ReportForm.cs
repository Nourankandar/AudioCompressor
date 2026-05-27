using System;
using System.Windows.Forms;
using System.Drawing;

namespace AudioCompressor.interfaces
{
    public class ReportForm : Form
    {
        public ReportForm(string fileName, long originalSize, long compressedSize, double timeTaken, string algo, int sampleRate, int bitRate)
        {
            // إعدادات النافذة اللطيفة لتناسب المظهر المظلم الاحترافي
            this.Text = "Compression Performance Report";
            this.Size = new Size(420, 500);

            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(39, 39, 42); // نفس لون الألواح في الواجهة الأساسية
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // حساب نسبة التوفير في الحجم (Space Savings)
            double savings = 0;
            if (originalSize > 0)
            {
                savings = ((originalSize - compressedSize) / (double)originalSize) * 100;
            }

            // تحويل الأحجام إلى كيلوبايت لتسهيل القراءة
            double origKB = originalSize / 1024.0;
            double compKB = compressedSize / 1024.0;

            // بناء نص التقرير المفصل بناءً على طلباتكِ بالتحديد
            Label lblReportText = new Label
            {
                AutoSize = true,
                Location = new Point(25, 25),
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.White
            };

            lblReportText.Text =
                $"╔══════════════════════════════════════╗\n" +
                $"   AUDIO COMPRESSION REPORT             \n" +
                $"╚══════════════════════════════════════╝\n\n" +
                $"📄 File Name: {fileName}\n\n" +

                $"📊 1. File Size (حجم الملف):\n" +
                $"    ▪ Original Size (قبل الضغط): {origKB:F2} KB\n" +
                $"    ▪ Compressed Size (بعد الضغط): {compKB:F2} KB\n\n" +

                $"📈 2. Space Savings (نسبة التوفير في الحجم):\n" +
                $"    ▪ Ratio: {savings:F2} %\n\n" +

                $"⏱️ 3. Execution Time (الزمن المستغرق):\n" +
                $"    ▪ Time Taken: {timeTaken:F4} Seconds\n\n" +

                $"⚙️ 4. Settings & Parameters (الخوارزمية وإعداداتها):\n" +
                $"    ▪ Used Algorithm: {algo}\n" +
                $"    ▪ Sampling Rate: {sampleRate} Hz\n" +
                $"    ▪ Sample Bit Rate: {bitRate} kbps";

            // إضافة زر لإغلاق التقرير بشكل أنيق في الأسفل
            Button btnClose = new Button
            {
                Text = "Close Report",
                Size = new Size(120, 35),
                Location = new Point(140, 290),
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

        private void InitializeComponent()
        {

        }
    }
}