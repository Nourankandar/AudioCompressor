using System;
using System.Windows.Forms;

namespace AudioCompressor.logic
{
    public class ClickHelper
    {
        private readonly AudioManager audioManager;
        private readonly CompressionEngine compressionEngine = new CompressionEngine();
        
        // متغيرات لحفظ مصفوفات البايتات المشفرة بعد الضغط لفكها لاحقاً
        private byte[] compressedAudioBytes; 
        private string activeAlgorithm;
        private int activeQuantizationBits;
        public long OriginalSize { get; private set; }
        public long CompressedSize { get; private set; }
        public double TimeTaken { get; private set; }
        public ClickHelper(AudioManager audioManager)
        {
            this.audioManager = audioManager;
        }

        // التقاط كليك زر الـ Browse
        public void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Audio Files (*.mp3;*.wav;*.wma)|*.mp3;*.wav;*.wma";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    audioManager.LoadSelectedFile(openFileDialog.FileName);
                }
            }
        }

        // التقاط كليك زر الـ Play
        public void BtnPlay_Click(object sender, EventArgs e)
        {
            audioManager.TogglePlay();
        }

        // التقاط كليك زر الـ Reset
        public void BtnReset_Click(object sender, EventArgs e)
        {
            audioManager.ResetAudio();
        }

        // التقاط أحداث السحب والإفلات وتمريرها
        public void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                audioManager.LoadSelectedFile(files[0]);
            }
        }

        public void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }
        public void BtnSave_Click(object sender, EventArgs e)
        {   
            if (!audioManager.IsFileLoaded)
            {
                MessageBox.Show("There is no file to save. Please load or compress an audio file first.", 
                                "No File Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Audio Files (*.mp3;*.wav;*.wma)|*.mp3;*.wav;*.wma";
                saveFileDialog.Title = "Save Compressed File";
                
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // استدعاء تابع الحفظ من الـ audioManager
                    audioManager.SaveAudioFile(saveFileDialog.FileName);
                }
            }
        }
        
        public void BtnCompress_Click(ComboBox cbAlgo, NumericUpDown numSampleRate, NumericUpDown numQuant, ProgressBar pb)
        {
            if (!audioManager.IsFileLoaded)
            {
                MessageBox.Show("Please load an audio file first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cbAlgo.SelectedItem == null)
            {
                MessageBox.Show("Please select a compression algorithm.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. قراءة الإعدادات المختارة من الواجهة
            activeAlgorithm = cbAlgo.SelectedItem.ToString();
            int sampleRate = (int)numSampleRate.Value;
            activeQuantizationBits = (int)numQuant.Value;

            try
            {
                pb.Value = 20;

                // 2. بدء حساب وقت التنفيذ بدقة باستخدام Stopwatch
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // 3. استدعاء محرك الضغط الحقيقي
                compressedAudioBytes = compressionEngine.CompressAudio(audioManager.SelectedFilePath, activeAlgorithm, sampleRate, activeQuantizationBits);

                stopwatch.Stop(); // إيقاف الحساب
                double secondsTaken = stopwatch.Elapsed.TotalSeconds;

                // 4. الحصول على الحجم الأصلي للملف الحقيقي
                long origSize = new System.IO.FileInfo(audioManager.SelectedFilePath).Length;
                long compSize = compressedAudioBytes.Length;

                // 5. حفظ البيانات الحقيقية في الـ AudioManager لكي يقرأها زر التقرير لاحقاً 🌟
                audioManager.UpdateCompressionStats(origSize, compSize, secondsTaken);

                pb.Value = 100;
                MessageBox.Show($"Audio compressed successfully using {activeAlgorithm}!\n" +
                                $"Original Size: {origSize / 1024} KB\n" +
                                $"Compressed Size: {compSize / 1024} KB\n" +
                                $"Time: {secondsTaken:F4} seconds.", 
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Compression error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                pb.Value = 0;
            }
        }        
        public void BtnDecompress_Click(ProgressBar pb)
        {
            if (compressedAudioBytes == null)
            {
                MessageBox.Show("No compressed data found. Please compress a file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                pb.Value = 40;
                
                // استعادة البيانات الأصلية
                byte[] decompressedBytes = compressionEngine.DecompressAudio(compressedAudioBytes, activeAlgorithm, activeQuantizationBits);
                
                pb.Value = 100;
                MessageBox.Show("Audio decompressed successfully! Signals restored.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Decompression error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                pb.Value = 0;
            }
        }
        // تابع جديد لتحديث بيانات التقرير فور انتهاء الضغط في الذاكرة
       

    }
}