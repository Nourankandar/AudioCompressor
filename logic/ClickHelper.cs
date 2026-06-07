

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioCompressor.logic
{
    public class ClickHelper
    {
        private readonly AudioManager audioManager;
        private readonly CompressionEngine compressionEngine = new CompressionEngine();

        // مسارات الملفات المؤقتة
        private string tempBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_compressed.bin");
        private string tempWavPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_decompressed.wav");

        // الخوارزمية النشطة — نحفظها لاستخدامها عند فك الضغط
        private string activeAlgorithm;

        // متغير الحفظ الذكي: ما هي آخر عملية (compress / decompress / none)
        private string lastAction = "none";

        // متغير الإلغاء
        private CancellationTokenSource cancellationTokenSource;

        public ClickHelper(AudioManager audioManager)
        {
            this.audioManager = audioManager;
        }

        // ===================================================
        //  أزرار التحكم الأساسية
        // ===================================================

        public void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Audio Files (*.mp3;*.wav;*.wma)|*.mp3;*.wav;*.wma";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    audioManager.LoadSelectedFile(openFileDialog.FileName);
                    lastAction = "none";
                }
            }
        }

        public void BtnPlay_Click(object sender, EventArgs e)
        {
            audioManager.TogglePlay();
        }

        public void BtnReset_Click(object sender, EventArgs e)
        {
            audioManager.ResetAudio();
            lastAction = "none";
            if (File.Exists(tempBinPath)) File.Delete(tempBinPath);
            if (File.Exists(tempWavPath)) File.Delete(tempWavPath);
        }

        public void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                audioManager.LoadSelectedFile(files[0]);
                lastAction = "none";
            }
        }

        public void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }

        // ===================================================
        //  زر الإلغاء
        // ===================================================

        public void BtnCancel_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
                cancellationTokenSource.Cancel();
        }

        // ===================================================
        //  زر Compress
        //  يستقبل الآن: cbAlgo, numSampleRate, numQuant, numStepSize, pb, btnCancel
        //  كل القيم تُستخدم فعلياً
        // ===================================================
        public async void BtnCompress_Click(
            ComboBox cbAlgo,
            NumericUpDown numSampleRate,
            NumericUpDown numQuant,
            NumericUpDown numStepSize,
            ProgressBar pb,
            Button btnCancel)
        {
            if (!audioManager.IsFileLoaded)
            {
                MessageBox.Show("Please load an audio file first!", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cbAlgo.SelectedItem == null)
            {
                MessageBox.Show("Please select a compression algorithm.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // قراءة كل الإعدادات من الواجهة — كلها تؤثر فعلياً
            activeAlgorithm      = cbAlgo.SelectedItem.ToString();
            int sampleRate       = (int)numSampleRate.Value;  // يؤثر فعلياً — Resample قبل الضغط
            int quantizationBits = (int)numQuant.Value;       // يؤثر في A-Law
            int stepSize         = (int)numStepSize.Value;    // يؤثر في Delta Modulation

            // حذف ملف WAV القديم لتجنب حفظ ملف خاطئ
            if (File.Exists(tempWavPath)) File.Delete(tempWavPath);

            // تهيئة الإلغاء
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            btnCancel.Visible = true;

            try
            {
                pb.Value = 20;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // تشغيل الضغط في الخلفية (async) حتى لا تتجمد الواجهة
                await Task.Run(() =>
                {
                    compressionEngine.CompressAudio(
                        audioManager.SelectedFilePath,
                        tempBinPath,
                        activeAlgorithm,
                        sampleRate,           // ← يؤثر فعلياً: Resample قبل الضغط
                        quantizationBits,
                        stepSize,
                        token);
                }, token);

                lastAction = "compress";
                stopwatch.Stop();
                double secondsTaken = stopwatch.Elapsed.TotalSeconds;

                long origSize = new FileInfo(audioManager.SelectedFilePath).Length;
                long compSize = new FileInfo(tempBinPath).Length;

                audioManager.UpdateCompressionStats(origSize, compSize, secondsTaken);

                pb.Value = 100;
                MessageBox.Show(
                    $"Audio compressed successfully using {activeAlgorithm}!\n" +
                    $"Original Size:   {origSize / 1024} KB\n" +
                    $"Compressed Size: {compSize / 1024} KB\n" +
                    $"Time: {secondsTaken:F4} seconds.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                pb.Value = 0;
                MessageBox.Show("Compression cancelled by user.", "Cancelled",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (File.Exists(tempBinPath)) File.Delete(tempBinPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Compression error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                pb.Value = 0;
            }
            finally
            {
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                btnCancel.Visible = false;
            }
        }

        // ===================================================
        //  زر Decompress
        //  لا يحتاج الإعدادات من الواجهة — يقرأها من الـ .bin مباشرة
        // ===================================================
        public void BtnDecompress_Click(ProgressBar pb)
        {
            if (!File.Exists(tempBinPath))
            {
                MessageBox.Show("No compressed file found. Please compress a file first.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(activeAlgorithm))
            {
                MessageBox.Show("Algorithm unknown. Please compress a file first.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                pb.Value = 40;

                // DecompressAudio يقرأ quantizationBits و stepSize من الـ .bin تلقائياً
                compressionEngine.DecompressAudio(tempBinPath, tempWavPath, activeAlgorithm);

                lastAction = "decompress";
                pb.Value = 100;
                MessageBox.Show(
                    "Audio decompressed successfully!\nSignals restored to a playable WAV format.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Decompression error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                pb.Value = 0;
            }
        }

        // ===================================================
        //  زر Save — حفظ ذكي بناءً على آخر عملية
        // ===================================================
        public void BtnSave_Click(object sender, EventArgs e)
        {
            if (lastAction == "none")
            {
                MessageBox.Show("Nothing to save! Please compress or decompress a file first.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                if (lastAction == "compress")
                {
                    saveFileDialog.Filter = "Compressed Binary (*.bin)|*.bin";
                    saveFileDialog.Title = "Save Compressed File";
                    saveFileDialog.DefaultExt = "bin";
                }
                else if (lastAction == "decompress")
                {
                    saveFileDialog.Filter = "Playable Audio (*.wav)|*.wav";
                    saveFileDialog.Title = "Save Decompressed Audio";
                    saveFileDialog.DefaultExt = "wav";
                }

                saveFileDialog.AddExtension = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (lastAction == "compress")
                            File.Copy(tempBinPath, saveFileDialog.FileName, true);
                        else if (lastAction == "decompress")
                            File.Copy(tempWavPath, saveFileDialog.FileName, true);

                        MessageBox.Show("File saved successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (IOException)
                    {
                        MessageBox.Show(
                            "Error: The file is currently in use. Please close it and try again.",
                            "File in Use", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving file: " + ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}