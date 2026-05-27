////////using System;
////////using System.Windows.Forms;

////////namespace AudioCompressor.logic
////////{
////////    public class ClickHelper
////////    {
////////        private readonly AudioManager audioManager;
////////        private readonly CompressionEngine compressionEngine = new CompressionEngine();

////////        // متغيرات لحفظ مصفوفات البايتات المشفرة بعد الضغط لفكها لاحقاً
////////        private byte[] compressedAudioBytes; 
////////        private string activeAlgorithm;
////////        private int activeQuantizationBits;
////////        public long OriginalSize { get; private set; }
////////        public long CompressedSize { get; private set; }
////////        public double TimeTaken { get; private set; }
////////        public ClickHelper(AudioManager audioManager)
////////        {
////////            this.audioManager = audioManager;
////////        }

////////        // التقاط كليك زر الـ Browse
////////        public void BtnBrowse_Click(object sender, EventArgs e)
////////        {
////////            using (OpenFileDialog openFileDialog = new OpenFileDialog())
////////            {
////////                openFileDialog.Filter = "Audio Files (*.mp3;*.wav;*.wma)|*.mp3;*.wav;*.wma";
////////                if (openFileDialog.ShowDialog() == DialogResult.OK)
////////                {
////////                    audioManager.LoadSelectedFile(openFileDialog.FileName);
////////                }
////////            }
////////        }

////////        // التقاط كليك زر الـ Play
////////        public void BtnPlay_Click(object sender, EventArgs e)
////////        {
////////            audioManager.TogglePlay();
////////        }

////////        // التقاط كليك زر الـ Reset
////////        public void BtnReset_Click(object sender, EventArgs e)
////////        {
////////            audioManager.ResetAudio();
////////        }

////////        // التقاط أحداث السحب والإفلات وتمريرها
////////        public void MainForm_DragDrop(object sender, DragEventArgs e)
////////        {
////////            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
////////            if (files != null && files.Length > 0)
////////            {
////////                audioManager.LoadSelectedFile(files[0]);
////////            }
////////        }

////////        public void MainForm_DragEnter(object sender, DragEventArgs e)
////////        {
////////            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
////////            else e.Effect = DragDropEffects.None;
////////        }
////////        public void BtnSave_Click(object sender, EventArgs e)
////////        {   
////////            if (!audioManager.IsFileLoaded)
////////            {
////////                MessageBox.Show("There is no file to save. Please load or compress an audio file first.", 
////////                                "No File Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
////////                return;
////////            }
////////            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
////////            {
////////                saveFileDialog.Filter = "Audio Files (*.mp3;*.wav;*.wma)|*.mp3;*.wav;*.wma";
////////                saveFileDialog.Title = "Save Compressed File";

////////                if (saveFileDialog.ShowDialog() == DialogResult.OK)
////////                {
////////                    // استدعاء تابع الحفظ من الـ audioManager
////////                    audioManager.SaveAudioFile(saveFileDialog.FileName);
////////                }
////////            }
////////        }

////////        public void BtnCompress_Click(ComboBox cbAlgo, NumericUpDown numSampleRate, NumericUpDown numQuant, ProgressBar pb)
////////        {
////////            if (!audioManager.IsFileLoaded)
////////            {
////////                MessageBox.Show("Please load an audio file first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
////////                return;
////////            }

////////            if (cbAlgo.SelectedItem == null)
////////            {
////////                MessageBox.Show("Please select a compression algorithm.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
////////                return;
////////            }

////////            // 1. قراءة الإعدادات المختارة من الواجهة
////////            activeAlgorithm = cbAlgo.SelectedItem.ToString();
////////            int sampleRate = (int)numSampleRate.Value;
////////            activeQuantizationBits = (int)numQuant.Value;

////////            try
////////            {
////////                pb.Value = 20;

////////                // 2. بدء حساب وقت التنفيذ بدقة باستخدام Stopwatch
////////                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

////////                // 3. استدعاء محرك الضغط الحقيقي
////////                compressedAudioBytes = compressionEngine.CompressAudio(audioManager.SelectedFilePath, activeAlgorithm, sampleRate, activeQuantizationBits);

////////                stopwatch.Stop(); // إيقاف الحساب
////////                double secondsTaken = stopwatch.Elapsed.TotalSeconds;

////////                // 4. الحصول على الحجم الأصلي للملف الحقيقي
////////                long origSize = new System.IO.FileInfo(audioManager.SelectedFilePath).Length;
////////                long compSize = compressedAudioBytes.Length;

////////                // 5. حفظ البيانات الحقيقية في الـ AudioManager لكي يقرأها زر التقرير لاحقاً 🌟
////////                audioManager.UpdateCompressionStats(origSize, compSize, secondsTaken);

////////                pb.Value = 100;
////////                MessageBox.Show($"Audio compressed successfully using {activeAlgorithm}!\n" +
////////                                $"Original Size: {origSize / 1024} KB\n" +
////////                                $"Compressed Size: {compSize / 1024} KB\n" +
////////                                $"Time: {secondsTaken:F4} seconds.", 
////////                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
////////            }
////////            catch (Exception ex)
////////            {
////////                MessageBox.Show("Compression error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
////////                pb.Value = 0;
////////            }
////////        }        
////////        public void BtnDecompress_Click(ProgressBar pb)
////////        {
////////            if (compressedAudioBytes == null)
////////            {
////////                MessageBox.Show("No compressed data found. Please compress a file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
////////                return;
////////            }

////////            try
////////            {
////////                pb.Value = 40;

////////                // استعادة البيانات الأصلية
////////                byte[] decompressedBytes = compressionEngine.DecompressAudio(compressedAudioBytes, activeAlgorithm, activeQuantizationBits);

////////                pb.Value = 100;
////////                MessageBox.Show("Audio decompressed successfully! Signals restored.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
////////            }
////////            catch (Exception ex)
////////            {
////////                MessageBox.Show("Decompression error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
////////                pb.Value = 0;
////////            }
////////        }
////////        // تابع جديد لتحديث بيانات التقرير فور انتهاء الضغط في الذاكرة


////////    }
////////}




//////using System;
//////using System.IO;
//////using System.Windows.Forms;

//////namespace AudioCompressor.logic
//////{
//////    public class ClickHelper
//////    {
//////        private readonly AudioManager audioManager;
//////        private readonly CompressionEngine compressionEngine = new CompressionEngine();

//////        // Use temporary file paths instead of byte arrays
//////        private string tempBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_compressed.bin");
//////        private string tempWavPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_decompressed.wav");

//////        private string activeAlgorithm;
//////        private int activeQuantizationBits;

//////        public long OriginalSize { get; private set; }
//////        public long CompressedSize { get; private set; }
//////        public double TimeTaken { get; private set; }

//////        public ClickHelper(AudioManager audioManager)
//////        {
//////            this.audioManager = audioManager;
//////        }

//////        public void BtnBrowse_Click(object sender, EventArgs e)
//////        {
//////            using (OpenFileDialog openFileDialog = new OpenFileDialog())
//////            {
//////                openFileDialog.Filter = "Audio Files (*.mp3;*.wav;*.wma)|*.mp3;*.wav;*.wma";
//////                if (openFileDialog.ShowDialog() == DialogResult.OK)
//////                {
//////                    audioManager.LoadSelectedFile(openFileDialog.FileName);
//////                }
//////            }
//////        }

//////        public void BtnPlay_Click(object sender, EventArgs e)
//////        {
//////            audioManager.TogglePlay();
//////        }

//////        public void BtnReset_Click(object sender, EventArgs e)
//////        {
//////            audioManager.ResetAudio();
//////            // Clean up temporary files on reset
//////            if (File.Exists(tempBinPath)) File.Delete(tempBinPath);
//////            if (File.Exists(tempWavPath)) File.Delete(tempWavPath);
//////        }

//////        public void MainForm_DragDrop(object sender, DragEventArgs e)
//////        {
//////            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
//////            if (files != null && files.Length > 0)
//////            {
//////                audioManager.LoadSelectedFile(files[0]);
//////            }
//////        }

//////        public void MainForm_DragEnter(object sender, DragEventArgs e)
//////        {
//////            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
//////            else e.Effect = DragDropEffects.None;
//////        }

//////        public void BtnSave_Click(object sender, EventArgs e)
//////        {
//////            if (!File.Exists(tempBinPath))
//////            {
//////                MessageBox.Show("There is no compressed file to save. Please compress an audio file first.",
//////                                "No File Ready", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//////                return;
//////            }
//////            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
//////            {
//////                // We are saving the custom compressed .bin file
//////                saveFileDialog.Filter = "Compressed Audio Bin (*.bin)|*.bin";
//////                saveFileDialog.Title = "Save Compressed File";

//////                if (saveFileDialog.ShowDialog() == DialogResult.OK)
//////                {
//////                    File.Copy(tempBinPath, saveFileDialog.FileName, true);
//////                    MessageBox.Show("Compressed file saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
//////                }
//////            }
//////        }

//////        public void BtnCompress_Click(ComboBox cbAlgo, NumericUpDown numSampleRate, NumericUpDown numQuant, ProgressBar pb)
//////        {
//////            if (!audioManager.IsFileLoaded)
//////            {
//////                MessageBox.Show("Please load an audio file first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//////                return;
//////            }

//////            if (cbAlgo.SelectedItem == null)
//////            {
//////                MessageBox.Show("Please select a compression algorithm.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//////                return;
//////            }

//////            activeAlgorithm = cbAlgo.SelectedItem.ToString();
//////            // Note: The sample rate is read automatically from the file now by NAudio, so we just pass the bits.
//////            activeQuantizationBits = (int)numQuant.Value;

//////            try
//////            {
//////                pb.Value = 20;
//////                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

//////                // Call the updated CompressionEngine method (Input Path, Output Temp Path, Algorithm, Bits)
//////                compressionEngine.CompressAudio(audioManager.SelectedFilePath, tempBinPath, activeAlgorithm, activeQuantizationBits);

//////                stopwatch.Stop();
//////                double secondsTaken = stopwatch.Elapsed.TotalSeconds;

//////                // Read file sizes straight from the hard drive
//////                long origSize = new FileInfo(audioManager.SelectedFilePath).Length;
//////                long compSize = new FileInfo(tempBinPath).Length;

//////                audioManager.UpdateCompressionStats(origSize, compSize, secondsTaken);

//////                pb.Value = 100;
//////                MessageBox.Show($"Audio compressed successfully using {activeAlgorithm}!\n" +
//////                                $"Original Size: {origSize / 1024} KB\n" +
//////                                $"Compressed Size: {compSize / 1024} KB\n" +
//////                                $"Time: {secondsTaken:F4} seconds.",
//////                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
//////            }
//////            catch (Exception ex)
//////            {
//////                MessageBox.Show("Compression error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//////                pb.Value = 0;
//////            }
//////        }

//////        public void BtnDecompress_Click(ProgressBar pb)
//////        {
//////            if (!File.Exists(tempBinPath))
//////            {
//////                MessageBox.Show("No compressed data found. Please compress a file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//////                return;
//////            }

//////            try
//////            {
//////                pb.Value = 40;

//////                // Call the updated DecompressAudio method (Input Temp Bin, Output Temp Wav, Algorithm, Bits)
//////                compressionEngine.DecompressAudio(tempBinPath, tempWavPath, activeAlgorithm, activeQuantizationBits);

//////                pb.Value = 100;
//////                MessageBox.Show("Audio decompressed successfully! Signals restored to a playable WAV format.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

//////                // Optional: You can automatically load the decompressed file into your audio player to test it
//////                // audioManager.LoadSelectedFile(tempWavPath); 
//////            }
//////            catch (Exception ex)
//////            {
//////                MessageBox.Show("Decompression error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//////                pb.Value = 0;
//////            }
//////        }
//////    }
//////}




////using System;
////using System.IO;
////using System.Windows.Forms;

////namespace AudioCompressor.logic
////{
////    public class ClickHelper
////    {
////        private readonly AudioManager audioManager;
////        private readonly CompressionEngine compressionEngine = new CompressionEngine();

////        // مسارات الملفات المؤقتة
////        private string tempBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_compressed.bin");
////        private string tempWavPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_decompressed.wav");

////        private string activeAlgorithm;
////        private int activeQuantizationBits;

////        // المتغير المسؤول عن الحفظ الذكي
////        private string lastAction = "none";

////        public long OriginalSize { get; private set; }
////        public long CompressedSize { get; private set; }
////        public double TimeTaken { get; private set; }

////        public ClickHelper(AudioManager audioManager)
////        {
////            this.audioManager = audioManager;
////        }

////        public void BtnBrowse_Click(object sender, EventArgs e)
////        {
////            using (OpenFileDialog openFileDialog = new OpenFileDialog())
////            {
////                openFileDialog.Filter = "Audio Files (*.mp3;*.wav;*.wma)|*.mp3;*.wav;*.wma";
////                if (openFileDialog.ShowDialog() == DialogResult.OK)
////                {
////                    audioManager.LoadSelectedFile(openFileDialog.FileName);
////                    lastAction = "none"; // تصفير الحالة عند فتح ملف جديد
////                }
////            }
////        }

////        public void BtnPlay_Click(object sender, EventArgs e)
////        {
////            audioManager.TogglePlay();
////        }

////        public void BtnReset_Click(object sender, EventArgs e)
////        {
////            audioManager.ResetAudio();
////            lastAction = "none";
////            // حذف الملفات المؤقتة عند عمل Reset
////            if (File.Exists(tempBinPath)) File.Delete(tempBinPath);
////            if (File.Exists(tempWavPath)) File.Delete(tempWavPath);
////        }

////        public void MainForm_DragDrop(object sender, DragEventArgs e)
////        {
////            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
////            if (files != null && files.Length > 0)
////            {
////                audioManager.LoadSelectedFile(files[0]);
////                lastAction = "none";
////            }
////        }

////        public void MainForm_DragEnter(object sender, DragEventArgs e)
////        {
////            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
////            else e.Effect = DragDropEffects.None;
////        }

////        // ----------------------------------------------------
////        // أزرار العمليات (ضغط، فك ضغط، حفظ)
////        // ----------------------------------------------------

////        public void BtnCompress_Click(ComboBox cbAlgo, NumericUpDown numSampleRate, NumericUpDown numQuant, ProgressBar pb)
////        {
////            if (!audioManager.IsFileLoaded)
////            {
////                MessageBox.Show("Please load an audio file first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
////                return;
////            }

////            if (cbAlgo.SelectedItem == null)
////            {
////                MessageBox.Show("Please select a compression algorithm.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
////                return;
////            }

////            activeAlgorithm = cbAlgo.SelectedItem.ToString();
////            activeQuantizationBits = (int)numQuant.Value;

////            // مسح الملف المفكوك القديم لتجنب حفظ ملف خاطئ
////            if (File.Exists(tempWavPath)) File.Delete(tempWavPath);

////            try
////            {
////                pb.Value = 20;
////                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

////                // ضغط الصوت وحفظه في ملف Bin مؤقت
////                compressionEngine.CompressAudio(audioManager.SelectedFilePath, tempBinPath, activeAlgorithm, activeQuantizationBits);

////                lastAction = "compress"; // تسجيل آخر عملية

////                stopwatch.Stop();
////                double secondsTaken = stopwatch.Elapsed.TotalSeconds;

////                long origSize = new FileInfo(audioManager.SelectedFilePath).Length;
////                long compSize = new FileInfo(tempBinPath).Length;

////                audioManager.UpdateCompressionStats(origSize, compSize, secondsTaken);

////                pb.Value = 100;
////                MessageBox.Show($"Audio compressed successfully using {activeAlgorithm}!\n" +
////                                $"Original Size: {origSize / 1024} KB\n" +
////                                $"Compressed Size: {compSize / 1024} KB\n" +
////                                $"Time: {secondsTaken:F4} seconds.",
////                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
////            }
////            catch (Exception ex)
////            {
////                MessageBox.Show("Compression error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
////                pb.Value = 0;
////            }
////        }

////        public void BtnDecompress_Click(ProgressBar pb)
////        {
////            if (!File.Exists(tempBinPath))
////            {
////                MessageBox.Show("No compressed data found. Please compress a file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
////                return;
////            }

////            try
////            {
////                pb.Value = 40;

////                // فك الضغط وحفظه في ملف Wav مؤقت
////                compressionEngine.DecompressAudio(tempBinPath, tempWavPath, activeAlgorithm, activeQuantizationBits);

////                lastAction = "decompress"; // تسجيل آخر عملية

////                pb.Value = 100;
////                MessageBox.Show("Audio decompressed successfully! Signals restored to a playable WAV format.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
////            }
////            catch (Exception ex)
////            {
////                MessageBox.Show("Decompression error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
////                pb.Value = 0;
////            }
////        }

////        public void BtnSave_Click(object sender, EventArgs e)
////        {
////            if (lastAction == "none")
////            {
////                MessageBox.Show("Nothing to save! Please compress or decompress a file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
////                return;
////            }

////            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
////            {
////                // تخصيص نوع الحفظ بناءً على آخر زر تم ضغطه
////                if (lastAction == "compress")
////                {
////                    saveFileDialog.Filter = "Compressed Binary (*.bin)|*.bin";
////                    saveFileDialog.Title = "Save Compressed File";
////                    saveFileDialog.DefaultExt = "bin";
////                }
////                else if (lastAction == "decompress")
////                {
////                    saveFileDialog.Filter = "Playable Audio (*.wav)|*.wav";
////                    saveFileDialog.Title = "Save Playable Audio";
////                    saveFileDialog.DefaultExt = "wav";
////                }

////                saveFileDialog.AddExtension = true;

////                if (saveFileDialog.ShowDialog() == DialogResult.OK)
////                {
////                    try
////                    {
////                        if (lastAction == "compress")
////                            File.Copy(tempBinPath, saveFileDialog.FileName, true);
////                        else if (lastAction == "decompress")
////                            File.Copy(tempWavPath, saveFileDialog.FileName, true);

////                        MessageBox.Show("File saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
////                    }
////                    catch (IOException)
////                    {
////                        MessageBox.Show("Error: The file is currently in use by another program. Please close it and try again.", "File in Use", MessageBoxButtons.OK, MessageBoxIcon.Error);
////                    }
////                    catch (Exception ex)
////                    {
////                        MessageBox.Show("Error saving file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
////                    }
////                }
////            }
////        }
////    }
////}







//using System;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Windows.Forms;

//namespace AudioCompressor.logic
//{
//    public class ClickHelper
//    {
//        private readonly AudioManager audioManager;
//        private readonly CompressionEngine compressionEngine = new CompressionEngine();

//        // مسارات الملفات المؤقتة
//        private string tempBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_compressed.bin");
//        private string tempWavPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_decompressed.wav");

//        private string activeAlgorithm;
//        private int activeQuantizationBits;

//        // المتغيرات المسؤولة عن الحفظ الذكي والإلغاء
//        private string lastAction = "none";
//        private CancellationTokenSource cancellationTokenSource;

//        public long OriginalSize { get; private set; }
//        public long CompressedSize { get; private set; }
//        public double TimeTaken { get; private set; }

//        public ClickHelper(AudioManager audioManager)
//        {
//            this.audioManager = audioManager;
//        }

//        // --- أزرار التحكم الأساسية (فتح، تشغيل، ريست) ---
//        public void BtnBrowse_Click(object sender, EventArgs e)
//        {
//            using (OpenFileDialog openFileDialog = new OpenFileDialog())
//            {
//                openFileDialog.Filter = "Audio Files (*.mp3;*.wav;*.wma)|*.mp3;*.wav;*.wma";
//                if (openFileDialog.ShowDialog() == DialogResult.OK)
//                {
//                    audioManager.LoadSelectedFile(openFileDialog.FileName);
//                    lastAction = "none";
//                }
//            }
//        }

//        public void BtnPlay_Click(object sender, EventArgs e)
//        {
//            audioManager.TogglePlay();
//        }

//        public void BtnReset_Click(object sender, EventArgs e)
//        {
//            audioManager.ResetAudio();
//            lastAction = "none";
//            if (File.Exists(tempBinPath)) File.Delete(tempBinPath);
//            if (File.Exists(tempWavPath)) File.Delete(tempWavPath);
//        }

//        public void MainForm_DragDrop(object sender, DragEventArgs e)
//        {
//            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
//            if (files != null && files.Length > 0)
//            {
//                audioManager.LoadSelectedFile(files[0]);
//                lastAction = "none";
//            }
//        }

//        public void MainForm_DragEnter(object sender, DragEventArgs e)
//        {
//            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
//            else e.Effect = DragDropEffects.None;
//        }

//        // ----------------------------------------------------
//        // زر الإلغاء الجديـــد (اربطه بزر الإلغاء في الفورم)
//        // ----------------------------------------------------
//        public void BtnCancel_Click(object sender, EventArgs e)
//        {
//            // إذا كانت العملية قيد التنفيذ، أرسل إشارة الإلغاء
//            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
//            {
//                cancellationTokenSource.Cancel();
//            }
//        }

//        // ----------------------------------------------------
//        // أزرار العمليات (ضغط، فك ضغط، حفظ)
//        // ----------------------------------------------------

//        // تم تحويلها إلى async لتشغيل الضغط في الخلفية وعدم تجميد الواجهة
//        public async void BtnCompress_Click(ComboBox cbAlgo, NumericUpDown numSampleRate, NumericUpDown numQuant, ProgressBar pb)
//        {
//            if (!audioManager.IsFileLoaded)
//            {
//                MessageBox.Show("Please load an audio file first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                return;
//            }

//            if (cbAlgo.SelectedItem == null)
//            {
//                MessageBox.Show("Please select a compression algorithm.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                return;
//            }

//            activeAlgorithm = cbAlgo.SelectedItem.ToString();
//            activeQuantizationBits = (int)numQuant.Value;

//            if (File.Exists(tempWavPath)) File.Delete(tempWavPath);

//            // تهيئة الـ Token الجديد للعملية الحالية
//            cancellationTokenSource = new CancellationTokenSource();
//            var token = cancellationTokenSource.Token;

//            try
//            {
//                pb.Value = 20;
//                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

//                // تشغيل محرك الضغط في مسار خلفي (Background Thread) لكي لا يتجمد البرنامج
//                await Task.Run(() =>
//                {
//                    compressionEngine.CompressAudio(audioManager.SelectedFilePath, tempBinPath, activeAlgorithm, activeQuantizationBits, token);
//                }, token);

//                // إذا وصلنا هنا، يعني العملية اكتملت بنجاح ولم يتم إلغاؤها
//                lastAction = "compress";
//                stopwatch.Stop();
//                double secondsTaken = stopwatch.Elapsed.TotalSeconds;

//                long origSize = new FileInfo(audioManager.SelectedFilePath).Length;
//                long compSize = new FileInfo(tempBinPath).Length;

//                audioManager.UpdateCompressionStats(origSize, compSize, secondsTaken);

//                pb.Value = 100;
//                MessageBox.Show($"Audio compressed successfully using {activeAlgorithm}!\n" +
//                                $"Original Size: {origSize / 1024} KB\n" +
//                                $"Compressed Size: {compSize / 1024} KB\n" +
//                                $"Time: {secondsTaken:F4} seconds.",
//                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
//            }
//            catch (OperationCanceledException) // اصطياد خطأ الإلغاء بشكل خاص
//            {
//                pb.Value = 0;
//                MessageBox.Show("Compression process was cancelled by the user.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);

//                // تنظيف الملف غير المكتمل
//                if (File.Exists(tempBinPath)) File.Delete(tempBinPath);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show("Compression error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                pb.Value = 0;
//            }
//            finally
//            {
//                // التخلص من كائن الإلغاء بعد الانتهاء
//                cancellationTokenSource?.Dispose();
//                cancellationTokenSource = null;
//            }
//        }

//        public void BtnDecompress_Click(ProgressBar pb)
//        {
//            if (!File.Exists(tempBinPath))
//            {
//                MessageBox.Show("No compressed data found. Please compress a file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                return;
//            }

//            try
//            {
//                pb.Value = 40;
//                compressionEngine.DecompressAudio(tempBinPath, tempWavPath, activeAlgorithm, activeQuantizationBits);
//                lastAction = "decompress";
//                pb.Value = 100;
//                MessageBox.Show("Audio decompressed successfully! Signals restored to a playable WAV format.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show("Decompression error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                pb.Value = 0;
//            }
//        }

//        public void BtnSave_Click(object sender, EventArgs e)
//        {
//            if (lastAction == "none")
//            {
//                MessageBox.Show("Nothing to save! Please compress or decompress a file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                return;
//            }

//            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
//            {
//                if (lastAction == "compress")
//                {
//                    saveFileDialog.Filter = "Compressed Binary (*.bin)|*.bin";
//                    saveFileDialog.Title = "Save Compressed File";
//                    saveFileDialog.DefaultExt = "bin";
//                }
//                else if (lastAction == "decompress")
//                {
//                    saveFileDialog.Filter = "Playable Audio (*.wav)|*.wav";
//                    saveFileDialog.Title = "Save Playable Audio";
//                    saveFileDialog.DefaultExt = "wav";
//                }

//                saveFileDialog.AddExtension = true;

//                if (saveFileDialog.ShowDialog() == DialogResult.OK)
//                {
//                    try
//                    {
//                        if (lastAction == "compress")
//                            File.Copy(tempBinPath, saveFileDialog.FileName, true);
//                        else if (lastAction == "decompress")
//                            File.Copy(tempWavPath, saveFileDialog.FileName, true);

//                        MessageBox.Show("File saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                    }
//                    catch (IOException)
//                    {
//                        MessageBox.Show("Error: The file is currently in use by another program. Please close it and try again.", "File in Use", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                    }
//                    catch (Exception ex)
//                    {
//                        MessageBox.Show("Error saving file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                    }
//                }
//            }
//        }
//    }
//}





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

        private string tempBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_compressed.bin");
        private string tempWavPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_decompressed.wav");

        private string activeAlgorithm;
        private int activeQuantizationBits;

        // State variables for Smart Save and Cancellation
        private string lastAction = "none";
        private CancellationTokenSource cancellationTokenSource;

        public long OriginalSize { get; private set; }
        public long CompressedSize { get; private set; }
        public double TimeTaken { get; private set; }

        public ClickHelper(AudioManager audioManager)
        {
            this.audioManager = audioManager;
        }

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

        // ----------------------------------------------------
        // Cancel Button Action
        // ----------------------------------------------------
        public void BtnCancel_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }
        }

        // ----------------------------------------------------
        // Compression & Decompression
        // ----------------------------------------------------

        // Notice the 'async' keyword and the 'Button btnCancel' parameter
        public async void BtnCompress_Click(ComboBox cbAlgo, NumericUpDown numSampleRate, NumericUpDown numQuant, ProgressBar pb, Button btnCancel)
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

            if (File.Exists(tempWavPath)) File.Delete(tempWavPath);

            // Initialize the cancellation token
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // Make the cancel button visible on the UI
            btnCancel.Visible = true;

            try
            {
                pb.Value = 20;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Run the heavy math on a background thread so the UI doesn't freeze
                await Task.Run(() =>
                {
                    compressionEngine.CompressAudio(audioManager.SelectedFilePath, tempBinPath, activeAlgorithm, activeQuantizationBits, token);
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
                // Clean up and hide the cancel button when done
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                btnCancel.Visible = false;
            }
        }



        public void BtnDecompress_Click(ProgressBar pb)
        {
            if (!File.Exists(tempBinPath))
            {
                MessageBox.Show("No compressed data found. Please compress a file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                pb.Value = 40;
                compressionEngine.DecompressAudio(tempBinPath, tempWavPath, activeAlgorithm, activeQuantizationBits);
                lastAction = "decompress";
                pb.Value = 100;
                MessageBox.Show("Audio decompressed successfully! Signals restored to a playable WAV format.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Decompression error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                pb.Value = 0;
            }
        }

        public void BtnSave_Click(object sender, EventArgs e)
        {
            if (lastAction == "none")
            {
                MessageBox.Show("Nothing to save! Please compress or decompress a file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    saveFileDialog.Title = "Save Playable Audio";
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

                        MessageBox.Show("File saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (IOException)
                    {
                        MessageBox.Show("Error: The file is currently in use by another program. Please close it and try again.", "File in Use", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}