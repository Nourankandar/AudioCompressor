using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AudioCompressor.interfaces;
namespace AudioCompressor.logic
{
    public class ClickHelper
    {
        private readonly AudioManager audioManager;
        private readonly CompressionEngine compressionEngine = new CompressionEngine();

        // مسارات الملفات المؤقتة
        public string tempBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_compressed.bin");
        public string tempWavPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_decompressed.wav");

        // الخوارزمية النشطة والإعدادات المحفوظة
        private string activeAlgorithm;
        private int activeQuantizationBits;

        // متغير الحفظ الذكي: ما هي آخر عملية (compress / decompress / none)
        public string lastAction = "none";

        // متغير الإلغاء
        private CancellationTokenSource cancellationTokenSource;

        public ClickHelper(AudioManager audioManager)
        {
            this.audioManager = audioManager;
        }

        public void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Audio & Compressed Files (*.mp3;*.wav;*.wma;*.bin)|*.mp3;*.wav;*.wma;*.bin";
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

        // public void BtnReset_Click(object sender, EventArgs e)
        // {
        //     audioManager.ResetAudio();
        //     lastAction = "none";
        //     if (File.Exists(tempBinPath)) File.Delete(tempBinPath);
        //     if (File.Exists(tempWavPath)) File.Delete(tempWavPath);
        // }

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

        public void BtnCancel_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
                cancellationTokenSource.Cancel();
        }

        // ===================================================
        //  زر الـ Compress المطور لإصلاح نقص البارامترات
        // ===================================================
        public async Task BtnCompress_Click(ComboBox cbAlgo, NumericUpDown numSampleRate, NumericUpDown numQuant, NumericUpDown numStep, ProgressBar pb, Button btnCancel)
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

            activeAlgorithm = cbAlgo.SelectedItem.ToString();
            activeQuantizationBits = (int)numQuant.Value;
            int targetSR = (int)numSampleRate.Value;
            int stepSize = (int)numStep.Value;

            if (File.Exists(tempWavPath)) File.Delete(tempWavPath);

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            try
            {
                pb.Value = 20;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // استدعاء الدالة بـ 7 بارامترات متطابقة تماماً مع محرك الضغط لمنع الـ Build Errors
                await Task.Run(() =>
                {
                    compressionEngine.CompressAudio(
                        audioManager.SelectedFilePath, 
                        tempBinPath, 
                        activeAlgorithm, 
                        targetSR, 
                        activeQuantizationBits, 
                        stepSize, 
                        token
                    );
                }, token);

                lastAction = "compress";
                stopwatch.Stop();
                double secondsTaken = stopwatch.Elapsed.TotalSeconds;

                long origSize = new FileInfo(audioManager.SelectedFilePath).Length;
                long compSize = new FileInfo(tempBinPath).Length;

                audioManager.UpdateCompressionStats(origSize, compSize, secondsTaken);

                pb.Value = 100;
                MessageBox.Show($"Audio compressed successfully using {activeAlgorithm}!\n" +
                                $"Original Size: {origSize / 1024} KB\n" +
                                $"Compressed Size: {compSize / 1024} KB\n" +
                                $"Time: {secondsTaken:F4} seconds.",
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                pb.Value = 0;
                lastAction = "none";
                MessageBox.Show("Compression process was cancelled by the user.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (File.Exists(tempBinPath)) File.Delete(tempBinPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Compression error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                pb.Value = 0;
            }
            finally
            {
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }

        public void BtnDecompress_Click(ProgressBar pb, ComboBox cbAlgo)
        {
            string targetBinPath = tempBinPath;
            bool isExternalBin = false;

            if (audioManager.IsFileLoaded && audioManager.SelectedFilePath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            {
                targetBinPath = audioManager.SelectedFilePath;
                isExternalBin = true;
            }
            else if (!File.Exists(targetBinPath))
            {
                MessageBox.Show("No compressed file found. Please compress a file or browse for a .bin file first.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string algoToUse = activeAlgorithm;

            if (isExternalBin)
            {
                if (cbAlgo.SelectedItem == null)
                {
                    MessageBox.Show("Please select the algorithm used to compress this .bin file from the dropdown list.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                algoToUse = cbAlgo.SelectedItem.ToString();
            }
            else if (string.IsNullOrEmpty(algoToUse))
            {
                MessageBox.Show("Algorithm unknown. Please compress a file first.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                pb.Value = 40;

                compressionEngine.DecompressAudio(targetBinPath, tempWavPath, algoToUse);

                lastAction = "decompress";
                pb.Value = 100;
                MessageBox.Show(
                    "Audio decompressed successfully!\nSignals restored to a playable WAV format.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                audioManager.LoadSelectedFile(tempWavPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Decompression error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                pb.Value = 0;
            }
        }

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
                        MessageBox.Show("Error: The file is currently in use. Please close it and try again.",
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