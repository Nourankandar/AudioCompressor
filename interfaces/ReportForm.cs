using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AudioCompressor.interfaces
{
    public class ReportForm : Form
    {
        private Color colorDarkBg = Color.FromArgb(20, 20, 23);
        private Color colorCardBg = Color.FromArgb(28, 28, 32);
        private Color colorBorder = Color.FromArgb(45, 45, 52);
        private Color colorTextMuted = Color.FromArgb(150, 150, 160);

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
            // ── إعدادات النافذة الأساسية ──
            this.Text = "Compression Analytics Report";
            this.Size = new Size(540, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = colorDarkBg;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 🌟 الحساب الرياضي الدقيق لنسبة التوفير بدون تصفير تلقائي
            double savings = 0.0;
            if (originalSize > 0)
            {
                savings = ((double)(originalSize - compressedSize) / originalSize) * 100.0;
            }

            double origKB = originalSize / 1024.0;
            double compKB = compressedSize / 1024.0;

            // ── 1. الهيدر (Header) ──
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(16, 16, 18) };
            pnlHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(38, 38, 42), 1))
                {
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
                e.Graphics.DrawString("COMPRESSION ANALYTICS", new Font("Segoe UI", 14, FontStyle.Bold), Brushes.White, 20, 22);
            };
            this.Controls.Add(pnlHeader);

            // ── 2. اللوحة المركزية للبيانات (Main Grid Container) ──
            Panel pnlGrid = new Panel { Location = new Point(20, 90), Size = new Size(485, 410), BackColor = colorCardBg };
            pnlGrid.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, pnlGrid.Width - 1, pnlGrid.Height - 1);
                using (GraphicsPath path = GetRoundedPath(rect, 8))
                {
                    pnlGrid.Region = new Region(path);
                    using (Pen borderPen = new Pen(colorBorder, 1f))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
            };
            this.Controls.Add(pnlGrid);

            // ── إعداد صفوف البيانات داخل الـ Grid ──
            int currentY = 20;
            int rowHeight = 35;
            int labelX = 20;
            int valueX = 220;

            void AddReportRow(string title, string value, Color valColor, bool isBold = false)
            {
                Label lblTitle = new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                    ForeColor = colorTextMuted,
                    Location = new Point(labelX, currentY),
                    AutoSize = true
                };

                Label lblValue = new Label
                {
                    Text = value,
                    Font = new Font("Segoe UI", 9.5f, isBold ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = valColor,
                    Location = new Point(valueX, currentY),
                    Width = 245,
                    AutoEllipsis = true
                };

                pnlGrid.Controls.Add(lblTitle);
                pnlGrid.Controls.Add(lblValue);
                currentY += rowHeight;
            }

            // قسم معلومات الملف
            AddReportRow("File Name:", fileName, Color.White);

            // خط فاصل داخلي ناعم
            AddSeparatorLine(pnlGrid, ref currentY);

            // قسم تحليل المساحة
            AddReportRow("Original Size:", $"{origKB:F2} KB", Color.White);
            AddReportRow("Compressed Size:", $"{compKB:F2} KB", Color.White);

            // عرض النسبة المئوية بلون أخضر محترم ومميز
            AddReportRow("Space Saved:", $"{savings:F2} %", Color.FromArgb(34, 197, 94), true);

            // خط فاصل داخلي ناعم
            AddSeparatorLine(pnlGrid, ref currentY);

            // قسم الأداء
            AddReportRow("Execution Time:", $"{timeTaken:F4} Seconds", Color.FromArgb(56, 189, 248));

            // خط فاصل داخلي ناعم
            AddSeparatorLine(pnlGrid, ref currentY);

            // قسم الإعدادات والخوارزمية
            AddReportRow("Algorithm:", algo.ToUpper(), Color.White, true);
            AddReportRow("Sample Rate:", $"{sampleRate} Hz", Color.White);

            switch (algo.ToUpper())
            {
                case "DPCM":
                    AddReportRow("Encoding Mode:", "Fixed Differential", Color.White);
                    break;
                case "DELTA MODULATION":
                    AddReportRow("Step Size:", stepSize.ToString(), Color.White);
                    break;
                case "NONLINEAR QUANTIZATION":
                    AddReportRow("Quantization Bits:", quantizationBits.ToString(), Color.White);
                    AddReportRow("Quantization Levels:", $"{Math.Pow(2, quantizationBits):F0}", Color.White);
                    break;
            }

            // ── 3. زر الإغلاق السفلي النظيف ──
            Button btnClose = new Button
            {
                Text = "Close Report",
                Location = new Point(190, 515),
                Width = 140,
                Height = 35,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(38, 38, 42),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = colorBorder;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 55);
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }
        private void AddSeparatorLine(Panel panel, ref int yPosition)
        {
            yPosition += 5;
            Panel line = new Panel
            {
                Location = new Point(20, yPosition),
                Size = new Size(445, 1),
                BackColor = Color.FromArgb(40, 40, 45)
            };
            panel.Controls.Add(line);
            yPosition += 15;
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void InitializeComponent() { }
    }
}