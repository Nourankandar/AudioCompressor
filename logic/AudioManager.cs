// using System;
// using System.Drawing;
// using System.Drawing.Drawing2D;
// using System.IO;
// using System.Windows.Forms;
// using NAudio.Wave;

// namespace AudioCompressor.logic
// {
//     public class AudioManager
//     {
//         // عناصر الواجهة التي سيقوم بتحديثها أو الرسم عليها
//         private Label lblFileName;
//         private Label lblFileSize;
//         private Label lblDuration;
//         private Panel pnlWaveform;
//         private Button btnPlay;
//         private Label lblSampleRate;
//         private Label lblChannels;
//         private Label lblBitrate;
//         private Label lblEncoding;
//         private System.Windows.Forms.Timer audioTimer;

//         // الحالات (States) الخاصة بالصوت
//         public string SelectedFilePath { get; private set; } = string.Empty;
//         public bool IsFileLoaded { get; private set; } = false;
//         public bool IsPlaying { get; private set; } = false;
//         private int animationOffset = 0;
//         private WaveOutEvent outputDevice;
//         private AudioFileReader audioFile;
//         public long OriginalSize { get; private set; }
//         public long CompressedSize { get; private set; }
//         public double TimeTaken { get; private set; }

//         // تابع لربط عناصر الواجهة بمدير الصوت (يتم استدعاؤه من الـ MainForm)
//         public void InitializeUIReferences(Label name, Label size, Label duration, Panel waveform, Button playBtn, 
//                                   Label sampleRate, Label channels, Label bitrate, Label encoding)
//         {
//             this.lblFileName = name;
//             this.lblFileSize = size;
//             this.lblDuration = duration;
//             this.pnlWaveform = waveform;
//             this.btnPlay = playBtn;
//             this.lblSampleRate = sampleRate;
//             this.lblChannels = channels;
//             this.lblBitrate = bitrate;
//             this.lblEncoding = encoding;

//             // إعداد التايمر الخاص بالتحريك داخل مدير الصوت
//             this.audioTimer = new System.Windows.Forms.Timer { Interval = 70 };
//             this.audioTimer.Tick += AudioTimer_Tick;
//         }

//         // تابع تحميل ومعالجة الملف الصوتي
//         public void LoadSelectedFile(string filePath)
//         {
//             try
//             {
//                 if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
//                     throw new FileNotFoundException("The selected file does not exist.");

//                 FileInfo fileInfo = new FileInfo(filePath);
//                 string ext = fileInfo.Extension.ToLower();

//                 if (ext != ".mp3" && ext != ".wav" && ext != ".wma")
//                     throw new InvalidDataException("Invalid format! Please select an MP3, WAV, or WMA file.");

//                 SelectedFilePath = fileInfo.FullName;
//                 audioFile = new AudioFileReader(SelectedFilePath);
//                 outputDevice = new WaveOutEvent();
//                 outputDevice.Init(audioFile);
//                 IsFileLoaded = true;

//                 // تحديث معلومات الصوت على الواجهة
//                 double sizeMB = GetFileSizeInMB(fileInfo);
//                 lblFileName.Text = $"File Name: {fileInfo.Name}";
//                 lblFileSize.Text = $"File Size: {sizeMB:F2} MB";
//                 lblDuration.Text = "Duration: 03:45 (Estimated)"; 
//                 var format = audioFile.WaveFormat;
//                 lblSampleRate.Text = $"Sample Rate: {format.SampleRate} Hz";
//                 lblChannels.Text = $"Channels: {format.Channels}";
//                 lblBitrate.Text = $"Bitrate: {(format.AverageBytesPerSecond * 8) / 1000} kbps";
//                 lblEncoding.Text = $"Encoding: {format.Encoding}";
//                 lblDuration.Text = $"Duration: {audioFile.TotalTime:mm\\:ss}"; // هنا التحديث الصحيح للمدة

//                 pnlWaveform.Invalidate();
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//             }
//         }

//         // حساب الحجم
//         public double GetFileSizeInMB(FileInfo fileInfo)
//         {
//             return (double)fileInfo.Length / (1024 * 1024);
//         }

//         // تشغيل وإيقاف الصوت (Toggle)
//         public void TogglePlay()
//         {
//             if (!IsFileLoaded || string.IsNullOrEmpty(SelectedFilePath))
//             {
//                 MessageBox.Show("Please load a valid audio file first!", "No File Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                 return;
//             }

//             IsPlaying = !IsPlaying;
//             btnPlay.Invalidate(); 

//             if (IsPlaying)
//             {
//                 audioTimer.Start();
//                 outputDevice.Play();
//             }
//             else
//             {
//                 audioTimer.Stop();
//                 outputDevice.Pause();
//             }
//         }

//         // تصفير الصوت (Reset)
//         public void ResetAudio()
//         {   
//             if (outputDevice != null) { outputDevice.Stop(); outputDevice.Dispose(); }
//             if (audioFile != null) { audioFile.Dispose(); }
//             IsPlaying = false;
//             IsFileLoaded = false;
//             SelectedFilePath = string.Empty;
//             animationOffset = 0;
//             audioTimer.Stop();

//             btnPlay.Invalidate();
//             lblFileName.Text = "File Name: No file selected";
//             lblFileSize.Text = "File Size: -- MB";
//             lblDuration.Text = "Duration: --:--";
//             pnlWaveform.Invalidate();
//         }

//         // منطق حركة الأمواج
//         private void AudioTimer_Tick(object sender, EventArgs e)
//         {
//             animationOffset += 4;
//             pnlWaveform.Invalidate();
//         }

//         // تابع رسم الأمواج الصوتيّة
//         public void DrawWaveform(Graphics g)
//         {
//             g.SmoothingMode = SmoothingMode.AntiAlias;
//             int midY = pnlWaveform.Height / 2;
//             int width = pnlWaveform.Width;

//             using (Pen basePen = new Pen(Color.FromArgb(45, 45, 50), 1))
//             {
//                 g.DrawLine(basePen, 0, midY, width, midY);
//             }

//             if (!IsFileLoaded || string.IsNullOrEmpty(SelectedFilePath))
//             {
//                 TextRenderer.DrawText(g, "No audio file loaded. Please browse to visualize.", new Font("Segoe UI", 10), new Point(width / 2 - 140, midY - 10), Color.FromArgb(63, 63, 70));
//                 return;
//             }

//             int barWidth = 4;
//             int barSpacing = 3;
//             Random rand = new Random(42);

//             using (LinearGradientBrush brush = new LinearGradientBrush(new Point(0, 0), new Point(0, pnlWaveform.Height), Color.FromArgb(34, 197, 94), Color.FromArgb(59, 130, 246)))
//             {
//                 for (int x = 15; x < width - 15; x += (barWidth + barSpacing))
//                 {
//                     int baseHeight = rand.Next(15, 110);
//                     if (IsPlaying)
//                     {
//                         baseHeight = (int)(baseHeight * (0.5 + 0.5 * Math.Sin((x + animationOffset) * 0.05)));
//                     }

//                     int top = midY - (baseHeight / 2);
//                     g.FillRectangle(brush, x, top, barWidth, baseHeight);
//                 }
//             }
//         }

//         // تابع رسم أيقونة زر التشغيل
//         public void DrawPlayButton(Button btn, Graphics g)
//         {
//             g.SmoothingMode = SmoothingMode.AntiAlias;

//             if (!IsPlaying)
//             {
//                 Point[] triangle = new Point[]
//                 {
//                     new Point(btn.Width / 2 - 5, btn.Height / 2 - 12),
//                     new Point(btn.Width / 2 - 5, btn.Height / 2 + 12),
//                     new Point(btn.Width / 2 + 12, btn.Height / 2)
//                 };
//                 using (SolidBrush brush = new SolidBrush(Color.White)) g.FillPolygon(brush, triangle);
//             }
//             else
//             {
//                 int rectSize = 18;
//                 Rectangle rect = new Rectangle(btn.Width / 2 - (rectSize / 2), btn.Height / 2 - (rectSize / 2), rectSize, rectSize);
//                 using (SolidBrush brush = new SolidBrush(Color.White)) g.FillRectangle(brush, rect);
//             }
//         }
//         public void SaveAudioFile(string savePath)
//         {
//             if (!IsFileLoaded) return;

//             try
//             {
//                 File.Copy(SelectedFilePath, savePath, true); 
//                 MessageBox.Show("File saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show("Error saving file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//             }
//         }
//         public void UpdateCompressionStats(long originalSize, long compressedSize, double seconds)
//         {
//             this.OriginalSize = originalSize;
//             this.CompressedSize = compressedSize;
//             this.TimeTaken = seconds;
//         }
//     }
// }






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

        // 🌟 المصفوفة التي ستحفظ قمم الصوت الحقيقية للرسم
        private float[] audioPeaks; 

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

                if (ext != ".mp3" && ext != ".wav" && ext != ".wma" && ext != ".bin")
                    throw new InvalidDataException("Invalid format! Please select an MP3, WAV, WMA, or BIN file.");

                SelectedFilePath = fileInfo.FullName;
                IsFileLoaded = true;

                // إذا كان الملف bin (مضغوط)، نقرأ معلوماته الأساسية فقط دون تشغيله في NAudio
                if (ext == ".bin")
                {
                    if (outputDevice != null) { outputDevice.Stop(); outputDevice.Dispose(); outputDevice = null; }
                    if (audioFile != null) { audioFile.Dispose(); audioFile = null; }

                    double binSizeMB = GetFileSizeInMB(fileInfo);
                    lblFileName.Text = $"File Name: {fileInfo.Name}";
                    lblFileSize.Text = $"File Size: {binSizeMB:F2} MB";
                    lblDuration.Text = "Duration: N/A (Compressed)";
                    lblSampleRate.Text = "Sample Rate: N/A";
                    lblChannels.Text = "Channels: N/A";
                    lblBitrate.Text = "Bitrate: N/A";
                    lblEncoding.Text = "Encoding: Binary Compressed";

                    pnlWaveform.Invalidate();
                    return;
                }

                // خلاف ذلك، نتعامل معه كملف صوتي طبيعي
                audioFile = new AudioFileReader(SelectedFilePath);
                outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);

                // 🌟 استدعاء دالة تحليل الصوت الحقيقي هنا
                GenerateRealWaveform();

                // تحديث معلومات الصوت على الواجهة
                double sizeMB = GetFileSizeInMB(fileInfo);
                lblFileName.Text = $"File Name: {fileInfo.Name}";
                lblFileSize.Text = $"File Size: {sizeMB:F2} MB";
                
                var format = audioFile.WaveFormat;
                lblSampleRate.Text = $"Sample Rate: {format.SampleRate} Hz";
                lblChannels.Text = $"Channels: {format.Channels}";
                lblBitrate.Text = $"Bitrate: {(format.AverageBytesPerSecond * 8) / 1000} kbps";
                lblEncoding.Text = $"Encoding: {format.Encoding}";
                lblDuration.Text = $"Duration: {audioFile.TotalTime:mm\\:ss}";

                pnlWaveform.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 🌟 الدالة الجديدة لاستخراج بيانات الموجة الحقيقية
        private void GenerateRealWaveform()
        {
            if (string.IsNullOrEmpty(SelectedFilePath)) return;

            int barWidth = 4;
            int barSpacing = 3;
            int totalBars = (pnlWaveform.Width - 30) / (barWidth + barSpacing); 
            if (totalBars <= 0) return;

            audioPeaks = new float[totalBars];

            // نفتح قارئ مؤقت فقط لغرض الرسم وتدميره مباشرة
            using (var tempReader = new AudioFileReader(SelectedFilePath))
            {
                int channels = tempReader.WaveFormat.Channels; 
                long totalSamples = tempReader.Length / 4; 
                int samplesPerBar = (int)(totalSamples / totalBars);
                
                // الحل السحري: إجبار حجم القراءة ليكون من مضاعفات عدد القنوات لمنع أخطاء NAudio
                samplesPerBar = samplesPerBar - (samplesPerBar % channels);
                if (samplesPerBar <= 0) samplesPerBar = channels; 

                float[] buffer = new float[samplesPerBar];

                for (int i = 0; i < totalBars; i++)
                {
                    int read = tempReader.Read(buffer, 0, buffer.Length);
                    if (read == 0) break;

                    float maxPeak = 0;
                    for (int j = 0; j < read; j++)
                    {
                        float absValue = Math.Abs(buffer[j]);
                        if (absValue > maxPeak) maxPeak = absValue;
                    }
                    
                    audioPeaks[i] = maxPeak; 
                }
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

            if (SelectedFilePath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Cannot play a compressed .bin file directly. Please decompress it first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            if (outputDevice != null) { outputDevice.Stop(); outputDevice.Dispose(); outputDevice = null; }
            if (audioFile != null) { audioFile.Dispose(); audioFile = null; }
            
            IsPlaying = false;
            IsFileLoaded = false;
            SelectedFilePath = string.Empty;
            animationOffset = 0;
            audioPeaks = null; // 🌟 تفريغ مصفوفة القمم لمنع تداخل الرسومات القديمة
            audioTimer.Stop();

            btnPlay.Invalidate();
            lblFileName.Text = "File Name: No file selected";
            lblFileSize.Text = "File Size: -- MB";
            lblDuration.Text = "Duration: --:--";
            lblSampleRate.Text = "Sample Rate: --";
            lblChannels.Text = "Channels: --";
            lblBitrate.Text = "Bitrate: --";
            lblEncoding.Text = "Encoding: --";
            pnlWaveform.Invalidate();
        }

        // منطق حركة الأمواج
        private void AudioTimer_Tick(object sender, EventArgs e)
        {
            animationOffset += 4;
            pnlWaveform.Invalidate(); // نطلب تحديث مساحة الرسم ليتحرك خط التشغيل
        }

        // تابع رسم الأمواج الصوتيّة (النسخة الاحترافية الثابتة مع خط التشغيل)
        public void DrawWaveform(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int midY = pnlWaveform.Height / 2;
            int width = pnlWaveform.Width;

            // 1. رسم خط المنتصف (الصفر)
            using (Pen basePen = new Pen(Color.FromArgb(45, 45, 50), 1))
            {
                g.DrawLine(basePen, 0, midY, width, midY);
            }

            // 2. التحقق من وجود بيانات (ورسائل للمستخدم)
            if (!IsFileLoaded || string.IsNullOrEmpty(SelectedFilePath))
            {
                TextRenderer.DrawText(g, "No audio file loaded. Please browse to visualize.", new Font("Segoe UI", 10), new Point(width / 2 - 140, midY - 10), Color.FromArgb(63, 63, 70));
                return;
            }

            if (SelectedFilePath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            {
                TextRenderer.DrawText(g, "Compressed binary file loaded. Decompress to visualize.", new Font("Segoe UI", 10), new Point(width / 2 - 160, midY - 10), Color.FromArgb(63, 63, 70));
                return;
            }

            if (audioPeaks == null) return;

            int barWidth = 4;
            int barSpacing = 3;
            int barIndex = 0;

            // 3. رسم الموجة الحقيقية الثابتة
            using (LinearGradientBrush brush = new LinearGradientBrush(new Point(0, 0), new Point(0, pnlWaveform.Height), Color.FromArgb(34, 197, 94), Color.FromArgb(59, 130, 246)))
            {
                for (int x = 15; x < width - 15; x += (barWidth + barSpacing))
                {
                    if (barIndex >= audioPeaks.Length) break;

                    // استخراج القمة الحقيقية للصوت
                    float peak = audioPeaks[barIndex];
                    int baseHeight = (int)(peak * (pnlWaveform.Height * 0.9)); 
                    
                    if (baseHeight < 2) baseHeight = 2; // الحد الأدنى للرسم

                    int top = midY - (baseHeight / 2); 
                    g.FillRectangle(brush, x, top, barWidth, baseHeight); 
                    
                    barIndex++;
                }
            }

            // 4. رسم "خط التشغيل" المتحرك الأحمر إذا كان الصوت يعمل
            if (IsPlaying && audioFile != null)
            {
                // حساب نسبة تقدم الصوت (من 0.0 إلى 1.0)
                float progress = (float)audioFile.Position / audioFile.Length;
                
                // تحويل النسبة إلى موقع عرضي على الشاشة
                int playheadX = 15 + (int)(progress * (width - 30)); 

                using (Pen playheadPen = new Pen(Color.FromArgb(239, 68, 68), 2))
                {
                    g.DrawLine(playheadPen, playheadX, 0, playheadX, pnlWaveform.Height);
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

        public void UpdateCompressionStats(long originalSize, long compressedSize, double seconds)
        {
            this.OriginalSize = originalSize;
            this.CompressedSize = compressedSize;
            this.TimeTaken = seconds;
        }
    }
}