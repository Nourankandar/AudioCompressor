using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using NAudio.Wave;

namespace AudioCompressor.logic
{
    public class AudioManager
    {
        // عناصر الواجهة التي سيقوم بتحديثها أو الرسم عليها
        private Label lblFileName;
        private Label lblFileSize;
        private Label lblDuration;
        private Panel pnlWaveform;
        private Button btnPlay;
        private Label lblSampleRate;
        private Label lblChannels;
        private Label lblBitrate;
        private Label lblEncoding;
        private System.Windows.Forms.Timer audioTimer;

        // الحالات (States) الخاصة بالصوت
        public string SelectedFilePath { get; private set; } = string.Empty;
        public bool IsFileLoaded { get; private set; } = false;
        public bool IsPlaying { get; private set; } = false;
        private int animationOffset = 0;
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        public long OriginalSize { get; private set; }
        public long CompressedSize { get; private set; }
        public double TimeTaken { get; private set; }

        // تابع لربط عناصر الواجهة بمدير الصوت (يتم استدعاؤه من الـ MainForm)
        public void InitializeUIReferences(Label name, Label size, Label duration, Panel waveform, Button playBtn, 
                                  Label sampleRate, Label channels, Label bitrate, Label encoding)
        {
            this.lblFileName = name;
            this.lblFileSize = size;
            this.lblDuration = duration;
            this.pnlWaveform = waveform;
            this.btnPlay = playBtn;
            this.lblSampleRate = sampleRate;
            this.lblChannels = channels;
            this.lblBitrate = bitrate;
            this.lblEncoding = encoding;

            // إعداد التايمر الخاص بالتحريك داخل مدير الصوت
            this.audioTimer = new System.Windows.Forms.Timer { Interval = 70 };
            this.audioTimer.Tick += AudioTimer_Tick;
        }

        // تابع تحميل ومعالجة الملف الصوتي
        public void LoadSelectedFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    throw new FileNotFoundException("The selected file does not exist.");

                FileInfo fileInfo = new FileInfo(filePath);
                string ext = fileInfo.Extension.ToLower();

                if (ext != ".mp3" && ext != ".wav" && ext != ".wma")
                    throw new InvalidDataException("Invalid format! Please select an MP3, WAV, or WMA file.");

                SelectedFilePath = fileInfo.FullName;
                audioFile = new AudioFileReader(SelectedFilePath);
                outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);
                IsFileLoaded = true;

                // تحديث معلومات الصوت على الواجهة
                double sizeMB = GetFileSizeInMB(fileInfo);
                lblFileName.Text = $"File Name: {fileInfo.Name}";
                lblFileSize.Text = $"File Size: {sizeMB:F2} MB";
                lblDuration.Text = "Duration: 03:45 (Estimated)"; 
                var format = audioFile.WaveFormat;
                lblSampleRate.Text = $"Sample Rate: {format.SampleRate} Hz";
                lblChannels.Text = $"Channels: {format.Channels}";
                lblBitrate.Text = $"Bitrate: {(format.AverageBytesPerSecond * 8) / 1000} kbps";
                lblEncoding.Text = $"Encoding: {format.Encoding}";
                lblDuration.Text = $"Duration: {audioFile.TotalTime:mm\\:ss}"; // هنا التحديث الصحيح للمدة

                pnlWaveform.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // حساب الحجم
        public double GetFileSizeInMB(FileInfo fileInfo)
        {
            return (double)fileInfo.Length / (1024 * 1024);
        }

        // تشغيل وإيقاف الصوت (Toggle)
        public void TogglePlay()
        {
            if (!IsFileLoaded || string.IsNullOrEmpty(SelectedFilePath))
            {
                MessageBox.Show("Please load a valid audio file first!", "No File Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            IsPlaying = !IsPlaying;
            btnPlay.Invalidate(); 

            if (IsPlaying)
            {
                audioTimer.Start();
                outputDevice.Play();
            }
            else
            {
                audioTimer.Stop();
                outputDevice.Pause();
            }
        }

        // تصفير الصوت (Reset)
        public void ResetAudio()
        {   
            if (outputDevice != null) { outputDevice.Stop(); outputDevice.Dispose(); }
            if (audioFile != null) { audioFile.Dispose(); }
            IsPlaying = false;
            IsFileLoaded = false;
            SelectedFilePath = string.Empty;
            animationOffset = 0;
            audioTimer.Stop();

            btnPlay.Invalidate();
            lblFileName.Text = "File Name: No file selected";
            lblFileSize.Text = "File Size: -- MB";
            lblDuration.Text = "Duration: --:--";
            pnlWaveform.Invalidate();
        }

        // منطق حركة الأمواج
        private void AudioTimer_Tick(object sender, EventArgs e)
        {
            animationOffset += 4;
            pnlWaveform.Invalidate();
        }

        // تابع رسم الأمواج الصوتيّة
        public void DrawWaveform(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int midY = pnlWaveform.Height / 2;
            int width = pnlWaveform.Width;

            using (Pen basePen = new Pen(Color.FromArgb(45, 45, 50), 1))
            {
                g.DrawLine(basePen, 0, midY, width, midY);
            }

            if (!IsFileLoaded || string.IsNullOrEmpty(SelectedFilePath))
            {
                TextRenderer.DrawText(g, "No audio file loaded. Please browse to visualize.", new Font("Segoe UI", 10), new Point(width / 2 - 140, midY - 10), Color.FromArgb(63, 63, 70));
                return;
            }

            int barWidth = 4;
            int barSpacing = 3;
            Random rand = new Random(42);

            using (LinearGradientBrush brush = new LinearGradientBrush(new Point(0, 0), new Point(0, pnlWaveform.Height), Color.FromArgb(34, 197, 94), Color.FromArgb(59, 130, 246)))
            {
                for (int x = 15; x < width - 15; x += (barWidth + barSpacing))
                {
                    int baseHeight = rand.Next(15, 110);
                    if (IsPlaying)
                    {
                        baseHeight = (int)(baseHeight * (0.5 + 0.5 * Math.Sin((x + animationOffset) * 0.05)));
                    }

                    int top = midY - (baseHeight / 2);
                    g.FillRectangle(brush, x, top, barWidth, baseHeight);
                }
            }
        }

        // تابع رسم أيقونة زر التشغيل
        public void DrawPlayButton(Button btn, Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (!IsPlaying)
            {
                Point[] triangle = new Point[]
                {
                    new Point(btn.Width / 2 - 5, btn.Height / 2 - 12),
                    new Point(btn.Width / 2 - 5, btn.Height / 2 + 12),
                    new Point(btn.Width / 2 + 12, btn.Height / 2)
                };
                using (SolidBrush brush = new SolidBrush(Color.White)) g.FillPolygon(brush, triangle);
            }
            else
            {
                int rectSize = 18;
                Rectangle rect = new Rectangle(btn.Width / 2 - (rectSize / 2), btn.Height / 2 - (rectSize / 2), rectSize, rectSize);
                using (SolidBrush brush = new SolidBrush(Color.White)) g.FillRectangle(brush, rect);
            }
        }
        public void SaveAudioFile(string savePath)
        {
            if (!IsFileLoaded) return;

            try
            {
                File.Copy(SelectedFilePath, savePath, true); 
                MessageBox.Show("File saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}