using System;
using System.Windows.Forms;
using System.Drawing;

namespace AudioCompressor.interfaces
{
    public class ReportForm : Form
    {
        public ReportForm(string fileName, long originalSize, long compressedSize, double timeTaken, string algo, int sampleRate, int bitRate)
        {
            this.Text = "Compression Report";
            this.Size = new Size(350, 300);
            this.StartPosition = FormStartPosition.CenterParent;

            double savings = ((originalSize - compressedSize) / (double)originalSize) * 100;

            Label lbl = new Label { AutoSize = true, Location = new Point(20, 20), Font = new Font("Segoe UI", 10) };
            lbl.Text = $"File: {fileName}\n\n" +
                       $"Algorithm: {algo}\n" +
                       $"Sample Rate: {sampleRate} Hz\n" +
                       $"Bit Rate: {bitRate} kbps\n\n" +
                       $"Original Size: {originalSize / 1024} KB\n" +
                       $"Compressed Size: {compressedSize / 1024} KB\n" +
                       $"Space Savings: {savings:F2}%\n\n" +
                       $"Time Taken: {timeTaken:F2} seconds";

            this.Controls.Add(lbl);
        }
    }
}