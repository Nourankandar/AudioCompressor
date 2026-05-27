//////using System;
//////using System.IO;

//////namespace AudioCompressor.logic
//////{
//////    public class CompressionEngine
//////    {
//////        // 1. التابع الرئيسي لضغط الصوت بناءً على الخوارزمية والإعدادات المختارة (الطلب 6)
//////        public byte[] CompressAudio(string inputPath, string algorithm, int sampleRate, int quantizationBits)
//////        {
//////            if (!File.Exists(inputPath))
//////                throw new FileNotFoundException("Input audio file not found.");

//////            // قراءة البيانات الخام للملف
//////            byte[] rawData = File.ReadAllBytes(inputPath);

//////            switch (algorithm.ToUpper())
//////            {
//////                case "DPCM":
//////                    return ExecuteDPCM(rawData);

//////                case "DELTA MODULATION":
//////                    return ExecuteDeltaModulation(rawData);

//////                case "NONLINEAR QUANTIZATION":
//////                    // التكميم غير الخطي يعتمد مباشرة على عدد البتات الممرر من الواجهة
//////                    return ExecuteNonlinearQuantization(rawData, quantizationBits);

//////                default:
//////                    return rawData; 
//////            }
//////        }

//////        // 2. تابع فك ضغط الصوت لاسترجاع الإشارة الصوتية الأصلية
//////        public byte[] DecompressAudio(byte[] compressedData, string algorithm, int quantizationBits)
//////        {
//////            switch (algorithm.ToUpper())
//////            {
//////                case "DPCM":
//////                    return DecompressDPCM(compressedData);

//////                case "DELTA MODULATION":
//////                    return DecompressDeltaModulation(compressedData);

//////                case "NONLINEAR QUANTIZATION":
//////                    return DecompressNonlinearQuantization(compressedData, quantizationBits);

//////                default:
//////                    return compressedData;
//////            }
//////        }

//////        // ----------------------------------------------------
//////        // الخوارزمية الأولى: Differential Pulse Code Modulation
//////        // ----------------------------------------------------
//////        private byte[] ExecuteDPCM(byte[] data)
//////        {
//////            if (data.Length == 0) return data;
//////            byte[] encoded = new byte[data.Length];

//////            // العينة الأولى تنزل كما هي كمرجع
//////            encoded[0] = data[0]; 

//////            for (int i = 1; i < data.Length; i++)
//////            {
//////                // حساب الفرق بين العينة الحالية والسابقة
//////                int diff = data[i] - data[i - 1];

//////                // تحويل الفرق إلى byte (إضافة 128 لتفادي القيم السالبة في مصفوفة الـ bytes)
//////                encoded[i] = (byte)(diff + 128);
//////            }
//////            return encoded;
//////        }

//////        private byte[] DecompressDPCM(byte[] data)
//////        {
//////            if (data.Length == 0) return data;
//////            byte[] decoded = new byte[data.Length];

//////            decoded[0] = data[0];
//////            for (int i = 1; i < data.Length; i++)
//////            {
//////                // استرجاع الفرق الحقيقي بطرح الـ 128
//////                int diff = data[i] - 128;

//////                // العينة الحالية = العينة السابقة + الفرق
//////                decoded[i] = (byte)(decoded[i - 1] + diff);
//////            }
//////            return decoded;
//////        }

//////        // ----------------------------------------------------
//////        // الخوارزمية الثانية: Delta Modulation (1-bit processing)
//////        // ----------------------------------------------------
//////        private byte[] ExecuteDeltaModulation(byte[] data)
//////        {
//////            if (data.Length == 0) return data;
//////            byte[] encoded = new byte[data.Length];
//////            byte stepSize = 4; // مقدار الخطوة الثابتة (Delta)
//////            byte predictedValue = data[0];

//////            encoded[0] = data[0];

//////            for (int i = 1; i < data.Length; i++)
//////            {
//////                if (data[i] >= predictedValue)
//////                {
//////                    encoded[i] = 1; // 1 تعني زيادة بمقدار الخطوة
//////                    predictedValue = (byte)Math.Min(255, predictedValue + stepSize);
//////                }
//////                else
//////                {
//////                    encoded[i] = 0; // 0 تعني نقصان بمقدار الخطوة
//////                    predictedValue = (byte)Math.Max(0, predictedValue - stepSize);
//////                }
//////            }
//////            return encoded;
//////        }

//////        private byte[] DecompressDeltaModulation(byte[] data)
//////        {
//////            if (data.Length == 0) return data;
//////            byte[] decoded = new byte[data.Length];
//////            byte stepSize = 4;

//////            decoded[0] = data[0];
//////            byte predictedValue = data[0];

//////            for (int i = 1; i < data.Length; i++)
//////            {
//////                if (data[i] == 1)
//////                {
//////                    predictedValue = (byte)Math.Min(255, predictedValue + stepSize);
//////                }
//////                else
//////                {
//////                    predictedValue = (byte)Math.Max(0, predictedValue - stepSize);
//////                }
//////                decoded[i] = predictedValue;
//////            }
//////            return decoded;
//////        }

//////        // ----------------------------------------------------
//////        // الخوارزمية الثالثة: Nonlinear Quantization (A-Law compression)
//////        // ----------------------------------------------------
//////        private byte[] ExecuteNonlinearQuantization(byte[] data, int bits)
//////        {
//////            byte[] encoded = new byte[data.Length];
//////            double A = 87.6; // المعامل العالمي القياسي لخوارزمية A-Law

//////            // حساب عدد مستويات التكميم بناءً على البتات المحددة من الواجهة (مثلاً 8 بت تعطي 256 مستوى)
//////            int levels = (int)Math.Pow(2, bits); 

//////            for (int i = 0; i < data.Length; i++)
//////            {
//////                // تحويل قيمة الـ byte إلى مجال بين -1.0 و 1.0 ل تطبيق اللوغاريتم
//////                double x = (data[i] - 128.0) / 128.0;
//////                double absX = Math.Abs(x);
//////                double y = 0;

//////                // تطبيق معادلة الضغط اللوغاريتمي غير الخطي لـ A-Law
//////                if (absX < (1.0 / A))
//////                {
//////                    y = (A * absX) / (1.0 + Math.Log(A));
//////                }
//////                else if (absX >= (1.0 / A) && absX <= 1.0)
//////                {
//////                    y = (1.0 + Math.Log(A * absX)) / (1.0 + Math.Log(A));
//////                }

//////                y = Math.Sign(x) * y;

//////                // إعادة التدريج (Quantization Mapping) للمستويات المطلوبة
//////                int quantized = (int)(((y + 1.0) / 2.0) * (levels - 1));
//////                encoded[i] = (byte)((quantized * 255) / (levels - 1));
//////            }
//////            return encoded;
//////        }

//////        private byte[] DecompressNonlinearQuantization(byte[] data, int bits)
//////        {
//////            byte[] decoded = new byte[data.Length];
//////            double A = 87.6;

//////            for (int i = 0; i < data.Length; i++)
//////            {
//////                // تحويل القيمة المشفرة إلى مجال -1.0 إلى 1.0
//////                double y = (data[i] - 128.0) / 128.0;
//////                double absY = Math.Abs(y);
//////                double x = 0;

//////                // عكس معادلة اللوغاريتم لاستعادة الإشارة الأصلية التقريبية
//////                if (absY < (1.0 / (1.0 + Math.Log(A))))
//////                {
//////                    x = absY * (1.0 + Math.Log(A)) / A;
//////                }
//////                else
//////                {
//////                    x = Math.Exp(absY * (1.0 + Math.Log(A)) - 1.0) / A;
//////                }

//////                x = Math.Sign(y) * x;
//////                decoded[i] = (byte)((x * 128.0) + 128.0);
//////            }
//////            return decoded;
//////        }
//////    }
//////}



////using System;
////using System.IO;
////using NAudio.Wave;

////namespace AudioCompressor.logic
////{
////    public class CompressionEngine
////    {
////        /// <summary>
////        /// Reads any audio file, compresses the pure audio data, and saves it to a custom binary file.
////        /// </summary>
////        public void CompressAudio(string inputWavPath, string outputCustomBinPath, string algorithm, int quantizationBits)
////        {
////            if (!File.Exists(inputWavPath))
////                throw new FileNotFoundException("Input audio file not found.");

////            // 1. Read pure audio samples using NAudio (ignores headers, extracts pure math)
////            using var reader = new AudioFileReader(inputWavPath);

////            // Convert float audio (-1.0 to 1.0) into 16-bit PCM (Short: -32768 to 32767) for precise integer math
////            float[] floatBuffer = new float[reader.Length / 4];
////            int samplesRead = reader.Read(floatBuffer, 0, floatBuffer.Length);

////            short[] pcmData = new short[samplesRead];
////            for (int i = 0; i < samplesRead; i++)
////            {
////                pcmData[i] = (short)(floatBuffer[i] * short.MaxValue);
////            }

////            // 2. Route to your specific algorithm
////            byte[] compressedData;
////            switch (algorithm.ToUpper())
////            {
////                case "DPCM":
////                    compressedData = ExecuteDPCM(pcmData);
////                    break;
////                case "DELTA MODULATION":
////                    compressedData = ExecuteDeltaModulation(pcmData);
////                    break;
////                case "NONLINEAR QUANTIZATION":
////                    compressedData = ExecuteNonlinearQuantization(pcmData, quantizationBits);
////                    break;
////                default:
////                    throw new ArgumentException("Unknown algorithm selected.");
////            }

////            // 3. Save the compressed data AND metadata to a custom file 
////            // We must save the SampleRate and Channels so we know how to rebuild the WAV later
////            using var fs = new FileStream(outputCustomBinPath, FileMode.Create);
////            using var bw = new BinaryWriter(fs);
////            bw.Write(reader.WaveFormat.SampleRate);
////            bw.Write(reader.WaveFormat.Channels);
////            bw.Write(compressedData.Length);
////            bw.Write(compressedData);
////        }

////        /// <summary>
////        /// Reads your custom binary file, reverses the algorithm, and exports a playable standard WAV file.
////        /// </summary>
////        public void DecompressAudio(string compressedBinPath, string outputWavPath, string algorithm, int quantizationBits)
////        {
////            if (!File.Exists(compressedBinPath))
////                throw new FileNotFoundException("Compressed .bin file not found.");

////            using var fs = new FileStream(compressedBinPath, FileMode.Open);
////            using var br = new BinaryReader(fs);

////            // 1. Read metadata from the custom file header we created
////            int sampleRate = br.ReadInt32();
////            int channels = br.ReadInt32();
////            int dataLength = br.ReadInt32();
////            byte[] compressedData = br.ReadBytes(dataLength);

////            // 2. Reverse the compression back to 16-bit PCM audio waves
////            short[] decompressedPcm;
////            switch (algorithm.ToUpper())
////            {
////                case "DPCM":
////                    decompressedPcm = DecompressDPCM(compressedData);
////                    break;
////                case "DELTA MODULATION":
////                    decompressedPcm = DecompressDeltaModulation(compressedData);
////                    break;
////                case "NONLINEAR QUANTIZATION":
////                    decompressedPcm = DecompressNonlinearQuantization(compressedData, quantizationBits);
////                    break;
////                default:
////                    throw new ArgumentException("Unknown algorithm selected.");
////            }

////            // 3. Save the rebuilt audio back to a standard WAV file so media players can run it
////            var format = new WaveFormat(sampleRate, 16, channels);
////            using var writer = new WaveFileWriter(outputWavPath, format);

////            // Convert short[] back to byte[] for the WaveFileWriter
////            byte[] finalWavBytes = new byte[decompressedPcm.Length * 2];
////            Buffer.BlockCopy(decompressedPcm, 0, finalWavBytes, 0, finalWavBytes.Length);
////            writer.Write(finalWavBytes, 0, finalWavBytes.Length);
////        }

////        // ----------------------------------------------------
////        // Algorithm 1: DPCM (16-bit to 8-bit difference)
////        // ----------------------------------------------------
////        private byte[] ExecuteDPCM(short[] data)
////        {
////            if (data.Length == 0) return Array.Empty<byte>();
////            byte[] encoded = new byte[data.Length];

////            // Scale first sample down to fit in 8-bits
////            encoded[0] = (byte)((data[0] >> 8) + 128);

////            for (int i = 1; i < data.Length; i++)
////            {
////                int diff = (data[i] >> 8) - (data[i - 1] >> 8);
////                // Clamp prevents overflow crashing
////                diff = Math.Clamp(diff + 128, 0, 255);
////                encoded[i] = (byte)diff;
////            }
////            return encoded;
////        }

////        private short[] DecompressDPCM(byte[] data)
////        {
////            if (data.Length == 0) return Array.Empty<short>();
////            short[] decoded = new short[data.Length];

////            int currentVal = data[0] - 128;
////            decoded[0] = (short)(currentVal << 8);

////            for (int i = 1; i < data.Length; i++)
////            {
////                int diff = data[i] - 128;
////                currentVal = Math.Clamp(currentVal + diff, -128, 127);
////                decoded[i] = (short)(currentVal << 8); // Shift back up to 16-bit range
////            }
////            return decoded;
////        }

////        // ----------------------------------------------------
////        // Algorithm 2: Delta Modulation (True 1-bit packing)
////        // ----------------------------------------------------
////        private byte[] ExecuteDeltaModulation(short[] data)
////        {
////            if (data.Length == 0) return Array.Empty<byte>();

////            // BIT PACKING: We are storing 8 samples inside a single byte. 
////            // This is why the output array is 1/8th the size!
////            int byteCount = (int)Math.Ceiling(data.Length / 8.0);
////            byte[] encoded = new byte[byteCount];

////            short stepSize = 1500; // Calibrated for 16-bit audio scale
////            short predictedValue = 0;

////            for (int i = 0; i < data.Length; i++)
////            {
////                int byteIndex = i / 8;
////                int bitIndex = i % 8;

////                if (data[i] >= predictedValue)
////                {
////                    // Push a '1' into the specific bit slot using Bitwise OR
////                    encoded[byteIndex] |= (byte)(1 << (7 - bitIndex));
////                    predictedValue = (short)Math.Min(short.MaxValue, predictedValue + stepSize);
////                }
////                else
////                {
////                    // Leave the bit as '0'
////                    predictedValue = (short)Math.Max(short.MinValue, predictedValue - stepSize);
////                }
////            }
////            return encoded;
////        }

////        private short[] DecompressDeltaModulation(byte[] data)
////        {
////            int sampleCount = data.Length * 8;
////            short[] decoded = new short[sampleCount];

////            short stepSize = 1500;
////            short predictedValue = 0;

////            for (int i = 0; i < sampleCount; i++)
////            {
////                int byteIndex = i / 8;
////                int bitIndex = i % 8;

////                // Read the specific bit using Bitwise AND
////                bool isOne = (data[byteIndex] & (1 << (7 - bitIndex))) != 0;

////                if (isOne)
////                {
////                    predictedValue = (short)Math.Min(short.MaxValue, predictedValue + stepSize);
////                }
////                else
////                {
////                    predictedValue = (short)Math.Max(short.MinValue, predictedValue - stepSize);
////                }
////                decoded[i] = predictedValue;
////            }
////            return decoded;
////        }

////        // ----------------------------------------------------
////        // Algorithm 3: Nonlinear Quantization (A-Law Mapping)
////        // ----------------------------------------------------
////        private byte[] ExecuteNonlinearQuantization(short[] data, int bits)
////        {
////            byte[] encoded = new byte[data.Length];
////            double A = 87.6;

////            for (int i = 0; i < data.Length; i++)
////            {
////                // Normalize 16-bit audio to -1.0 to 1.0 for the logarithm
////                double x = data[i] / (double)short.MaxValue;
////                double absX = Math.Abs(x);
////                double y = 0;

////                if (absX < (1.0 / A)) y = (A * absX) / (1.0 + Math.Log(A));
////                else y = (1.0 + Math.Log(A * absX)) / (1.0 + Math.Log(A));

////                y = Math.Sign(x) * y;

////                // Map the float down to an 8-bit byte (0 to 255)
////                int quantized = (int)(((y + 1.0) / 2.0) * 255);
////                encoded[i] = (byte)Math.Clamp(quantized, 0, 255);
////            }
////            return encoded;
////        }

////        private short[] DecompressNonlinearQuantization(byte[] data, int bits)
////        {
////            short[] decoded = new short[data.Length];
////            double A = 87.6;

////            for (int i = 0; i < data.Length; i++)
////            {
////                // De-normalize from 0-255 back to -1.0 to 1.0
////                double y = ((data[i] / 255.0) * 2.0) - 1.0;
////                double absY = Math.Abs(y);
////                double x = 0;

////                if (absY < (1.0 / (1.0 + Math.Log(A)))) x = absY * (1.0 + Math.Log(A)) / A;
////                else x = Math.Exp(absY * (1.0 + Math.Log(A)) - 1.0) / A;

////                x = Math.Sign(y) * x;

////                // Scale back up to 16-bit audio range
////                decoded[i] = (short)Math.Clamp(x * short.MaxValue, short.MinValue, short.MaxValue);
////            }
////            return decoded;
////        }
////    }
////}




//using System;
//using System.IO;
//using System.Threading;
//using NAudio.Wave;

//namespace AudioCompressor.logic
//{
//    public class CompressionEngine
//    {
//        /// <summary>
//        /// يقرأ ملف الصوت الأصلي، يطبق خوارزمية الضغط، ويحفظ النتيجة في ملف .bin مخصص
//        /// تم إضافة token لدعم الإلغاء أثناء التنفيذ
//        /// </summary>
//        public void CompressAudio(string inputWavPath, string outputCustomBinPath, string algorithm, int quantizationBits, CancellationToken token)
//        {
//            if (!File.Exists(inputWavPath))
//                throw new FileNotFoundException("Input audio file not found.");

//            using var reader = new AudioFileReader(inputWavPath);
//            float[] floatBuffer = new float[reader.Length / 4];
//            int samplesRead = reader.Read(floatBuffer, 0, floatBuffer.Length);

//            short[] pcmData = new short[samplesRead];
//            for (int i = 0; i < samplesRead; i++)
//            {
//                // فحص الإلغاء أثناء قراءة العينات (إذا كان الملف ضخماً جداً)
//                if (i % 10000 == 0) token.ThrowIfCancellationRequested();
//                pcmData[i] = (short)(floatBuffer[i] * short.MaxValue);
//            }

//            byte[] compressedData;
//            switch (algorithm.ToUpper())
//            {
//                case "DPCM":
//                    compressedData = ExecuteDPCM(pcmData, token);
//                    break;
//                case "DELTA MODULATION":
//                    compressedData = ExecuteDeltaModulation(pcmData, token);
//                    break;
//                case "NONLINEAR QUANTIZATION":
//                    compressedData = ExecuteNonlinearQuantization(pcmData, quantizationBits, token);
//                    break;
//                default:
//                    throw new ArgumentException("Unknown algorithm selected.");
//            }

//            // فحص الإلغاء قبل البدء بكتابة الملف النهائي
//            token.ThrowIfCancellationRequested();

//            using var fs = new FileStream(outputCustomBinPath, FileMode.Create);
//            using var bw = new BinaryWriter(fs);
//            bw.Write(reader.WaveFormat.SampleRate);
//            bw.Write(reader.WaveFormat.Channels);
//            bw.Write(compressedData.Length);
//            bw.Write(compressedData);
//        }

//        public void DecompressAudio(string compressedBinPath, string outputWavPath, string algorithm, int quantizationBits)
//        {
//            if (!File.Exists(compressedBinPath))
//                throw new FileNotFoundException("Compressed .bin file not found.");

//            using var fs = new FileStream(compressedBinPath, FileMode.Open);
//            using var br = new BinaryReader(fs);

//            int sampleRate = br.ReadInt32();
//            int channels = br.ReadInt32();
//            int dataLength = br.ReadInt32();
//            byte[] compressedData = br.ReadBytes(dataLength);

//            short[] decompressedPcm;
//            switch (algorithm.ToUpper())
//            {
//                case "DPCM":
//                    decompressedPcm = DecompressDPCM(compressedData);
//                    break;
//                case "DELTA MODULATION":
//                    decompressedPcm = DecompressDeltaModulation(compressedData);
//                    break;
//                case "NONLINEAR QUANTIZATION":
//                    decompressedPcm = DecompressNonlinearQuantization(compressedData, quantizationBits);
//                    break;
//                default:
//                    throw new ArgumentException("Unknown algorithm selected.");
//            }

//            var format = new WaveFormat(sampleRate, 16, channels);
//            using var writer = new WaveFileWriter(outputWavPath, format);

//            byte[] finalWavBytes = new byte[decompressedPcm.Length * 2];
//            Buffer.BlockCopy(decompressedPcm, 0, finalWavBytes, 0, finalWavBytes.Length);
//            writer.Write(finalWavBytes, 0, finalWavBytes.Length);
//        }

//        // ----------------------------------------------------
//        // Algorithm 1: DPCM 
//        // ----------------------------------------------------
//        private byte[] ExecuteDPCM(short[] data, CancellationToken token)
//        {
//            if (data.Length == 0) return Array.Empty<byte>();
//            byte[] encoded = new byte[data.Length];

//            encoded[0] = (byte)((data[0] >> 8) + 128);

//            for (int i = 1; i < data.Length; i++)
//            {
//                // إيقاف العملية فوراً إذا طلب المستخدم الإلغاء
//                if (i % 5000 == 0) token.ThrowIfCancellationRequested();

//                int diff = (data[i] >> 8) - (data[i - 1] >> 8);
//                diff = Math.Clamp(diff + 128, 0, 255);
//                encoded[i] = (byte)diff;
//            }
//            return encoded;
//        }

//        private short[] DecompressDPCM(byte[] data)
//        {
//            if (data.Length == 0) return Array.Empty<short>();
//            short[] decoded = new short[data.Length];

//            int currentVal = data[0] - 128;
//            decoded[0] = (short)(currentVal << 8);

//            for (int i = 1; i < data.Length; i++)
//            {
//                int diff = data[i] - 128;
//                currentVal = Math.Clamp(currentVal + diff, -128, 127);
//                decoded[i] = (short)(currentVal << 8);
//            }
//            return decoded;
//        }

//        // ----------------------------------------------------
//        // Algorithm 2: Delta Modulation 
//        // ----------------------------------------------------
//        private byte[] ExecuteDeltaModulation(short[] data, CancellationToken token)
//        {
//            if (data.Length == 0) return Array.Empty<byte>();

//            int byteCount = (int)Math.Ceiling(data.Length / 8.0);
//            byte[] encoded = new byte[byteCount];

//            short stepSize = 1500;
//            short predictedValue = 0;

//            for (int i = 0; i < data.Length; i++)
//            {
//                // إيقاف العملية فوراً إذا طلب المستخدم الإلغاء
//                if (i % 5000 == 0) token.ThrowIfCancellationRequested();

//                int byteIndex = i / 8;
//                int bitIndex = i % 8;

//                if (data[i] >= predictedValue)
//                {
//                    encoded[byteIndex] |= (byte)(1 << (7 - bitIndex));
//                    predictedValue = (short)Math.Min(short.MaxValue, predictedValue + stepSize);
//                }
//                else
//                {
//                    predictedValue = (short)Math.Max(short.MinValue, predictedValue - stepSize);
//                }
//            }
//            return encoded;
//        }

//        private short[] DecompressDeltaModulation(byte[] data)
//        {
//            int sampleCount = data.Length * 8;
//            short[] decoded = new short[sampleCount];

//            short stepSize = 1500;
//            short predictedValue = 0;

//            for (int i = 0; i < sampleCount; i++)
//            {
//                int byteIndex = i / 8;
//                int bitIndex = i % 8;

//                bool isOne = (data[byteIndex] & (1 << (7 - bitIndex))) != 0;

//                if (isOne)
//                {
//                    predictedValue = (short)Math.Min(short.MaxValue, predictedValue + stepSize);
//                }
//                else
//                {
//                    predictedValue = (short)Math.Max(short.MinValue, predictedValue - stepSize);
//                }
//                decoded[i] = predictedValue;
//            }
//            return decoded;
//        }

//        // ----------------------------------------------------
//        // Algorithm 3: Nonlinear Quantization (A-Law Mapping)
//        // ----------------------------------------------------
//        private byte[] ExecuteNonlinearQuantization(short[] data, int bits, CancellationToken token)
//        {
//            byte[] encoded = new byte[data.Length];
//            double A = 87.6;

//            for (int i = 0; i < data.Length; i++)
//            {
//                // إيقاف العملية فوراً إذا طلب المستخدم الإلغاء
//                if (i % 5000 == 0) token.ThrowIfCancellationRequested();

//                double x = data[i] / (double)short.MaxValue;
//                double absX = Math.Abs(x);
//                double y = 0;

//                if (absX < (1.0 / A)) y = (A * absX) / (1.0 + Math.Log(A));
//                else y = (1.0 + Math.Log(A * absX)) / (1.0 + Math.Log(A));

//                y = Math.Sign(x) * y;

//                int quantized = (int)(((y + 1.0) / 2.0) * 255);
//                encoded[i] = (byte)Math.Clamp(quantized, 0, 255);
//            }
//            return encoded;
//        }

//        private short[] DecompressNonlinearQuantization(byte[] data, int bits)
//        {
//            short[] decoded = new short[data.Length];
//            double A = 87.6;

//            for (int i = 0; i < data.Length; i++)
//            {
//                double y = ((data[i] / 255.0) * 2.0) - 1.0;
//                double absY = Math.Abs(y);
//                double x = 0;

//                if (absY < (1.0 / (1.0 + Math.Log(A)))) x = absY * (1.0 + Math.Log(A)) / A;
//                else x = Math.Exp(absY * (1.0 + Math.Log(A)) - 1.0) / A;

//                x = Math.Sign(y) * x;

//                decoded[i] = (short)Math.Clamp(x * short.MaxValue, short.MinValue, short.MaxValue);
//            }
//            return decoded;
//        }
//    }
//}





using System;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace AudioCompressor.logic
{
    public class CompressionEngine
    {
        public void CompressAudio(string inputWavPath, string outputCustomBinPath, string algorithm, int quantizationBits, CancellationToken token)
        {
            if (!File.Exists(inputWavPath))
                throw new FileNotFoundException("Input audio file not found.");

            // 1. Read pure audio samples using NAudio
            using var reader = new AudioFileReader(inputWavPath);
            float[] floatBuffer = new float[reader.Length / 4];
            int samplesRead = reader.Read(floatBuffer, 0, floatBuffer.Length);

            short[] pcmData = new short[samplesRead];
            for (int i = 0; i < samplesRead; i++)
            {
                // Check for cancellation during read for massive files
                if (i % 10000 == 0) token.ThrowIfCancellationRequested();
                pcmData[i] = (short)(floatBuffer[i] * short.MaxValue);
            }

            // 2. Route to specific algorithm
            byte[] compressedData;
            switch (algorithm.ToUpper())
            {
                case "DPCM":
                    compressedData = ExecuteDPCM(pcmData, token);
                    break;
                case "DELTA MODULATION":
                    compressedData = ExecuteDeltaModulation(pcmData, token);
                    break;
                case "NONLINEAR QUANTIZATION":
                    compressedData = ExecuteNonlinearQuantization(pcmData, quantizationBits, token);
                    break;
                default:
                    throw new ArgumentException("Unknown algorithm selected.");
            }

            try
            {

            token.ThrowIfCancellationRequested();

            }catch (Exception ex)
            {
                // Handle cancellation gracefully (e.g., delete partial file, log, etc.)
                if (File.Exists(outputCustomBinPath))
                    File.Delete(outputCustomBinPath);
                //throw; // Re-throw to let the caller know it was cancelled
            }

            // 3. Save compressed data with custom metadata headers
            using var fs = new FileStream(outputCustomBinPath, FileMode.Create);
            using var bw = new BinaryWriter(fs);
            bw.Write(reader.WaveFormat.SampleRate);
            bw.Write(reader.WaveFormat.Channels);
            bw.Write(compressedData.Length);
            bw.Write(compressedData);
        }

        public void DecompressAudio(string compressedBinPath, string outputWavPath, string algorithm, int quantizationBits)
        {
            if (!File.Exists(compressedBinPath))
                throw new FileNotFoundException("Compressed .bin file not found.");

            using var fs = new FileStream(compressedBinPath, FileMode.Open);
            using var br = new BinaryReader(fs);

            int sampleRate = br.ReadInt32();
            int channels = br.ReadInt32();
            int dataLength = br.ReadInt32();
            byte[] compressedData = br.ReadBytes(dataLength);

            short[] decompressedPcm;
            switch (algorithm.ToUpper())
            {
                case "DPCM":
                    decompressedPcm = DecompressDPCM(compressedData);
                    break;
                case "DELTA MODULATION":
                    decompressedPcm = DecompressDeltaModulation(compressedData);
                    break;
                case "NONLINEAR QUANTIZATION":
                    decompressedPcm = DecompressNonlinearQuantization(compressedData, quantizationBits);
                    break;
                default:
                    throw new ArgumentException("Unknown algorithm selected.");
            }

            var format = new WaveFormat(sampleRate, 16, channels);
            using var writer = new WaveFileWriter(outputWavPath, format);

            byte[] finalWavBytes = new byte[decompressedPcm.Length * 2];
            Buffer.BlockCopy(decompressedPcm, 0, finalWavBytes, 0, finalWavBytes.Length);
            writer.Write(finalWavBytes, 0, finalWavBytes.Length);
        }

        // ----------------------------------------------------
        // Algorithm 1: DPCM 
        // ----------------------------------------------------
        private byte[] ExecuteDPCM(short[] data, CancellationToken token)
        {
            if (data.Length == 0) return Array.Empty<byte>();
            byte[] encoded = new byte[data.Length];

            encoded[0] = (byte)((data[0] >> 8) + 128);

            for (int i = 1; i < data.Length; i++)
            {
                // Halt instantly if the user clicked cancel
                if (i % 5000 == 0) token.ThrowIfCancellationRequested();

                int diff = (data[i] >> 8) - (data[i - 1] >> 8);
                diff = Math.Clamp(diff + 128, 0, 255);
                encoded[i] = (byte)diff;
            }
            return encoded;
        }

        private short[] DecompressDPCM(byte[] data)
        {
            if (data.Length == 0) return Array.Empty<short>();
            short[] decoded = new short[data.Length];

            int currentVal = data[0] - 128;
            decoded[0] = (short)(currentVal << 8);

            for (int i = 1; i < data.Length; i++)
            {
                int diff = data[i] - 128;
                currentVal = Math.Clamp(currentVal + diff, -128, 127);
                decoded[i] = (short)(currentVal << 8);
            }
            return decoded;
        }

        // ----------------------------------------------------
        // Algorithm 2: Delta Modulation 
        // ----------------------------------------------------
        private byte[] ExecuteDeltaModulation(short[] data, CancellationToken token)
        {
            if (data.Length == 0) return Array.Empty<byte>();

            int byteCount = (int)Math.Ceiling(data.Length / 8.0);
            byte[] encoded = new byte[byteCount];

            short stepSize = 1500;
            short predictedValue = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (i % 5000 == 0) token.ThrowIfCancellationRequested();

                int byteIndex = i / 8;
                int bitIndex = i % 8;

                if (data[i] >= predictedValue)
                {
                    encoded[byteIndex] |= (byte)(1 << (7 - bitIndex));
                    predictedValue = (short)Math.Min(short.MaxValue, predictedValue + stepSize);
                }
                else
                {
                    predictedValue = (short)Math.Max(short.MinValue, predictedValue - stepSize);
                }
            }
            return encoded;
        }

        private short[] DecompressDeltaModulation(byte[] data)
        {
            int sampleCount = data.Length * 8;
            short[] decoded = new short[sampleCount];

            short stepSize = 1500;
            short predictedValue = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                int byteIndex = i / 8;
                int bitIndex = i % 8;

                bool isOne = (data[byteIndex] & (1 << (7 - bitIndex))) != 0;

                if (isOne)
                {
                    predictedValue = (short)Math.Min(short.MaxValue, predictedValue + stepSize);
                }
                else
                {
                    predictedValue = (short)Math.Max(short.MinValue, predictedValue - stepSize);
                }
                decoded[i] = predictedValue;
            }
            return decoded;
        }

        // ----------------------------------------------------
        // Algorithm 3: Nonlinear Quantization (A-Law Mapping)
        // ----------------------------------------------------
        private byte[] ExecuteNonlinearQuantization(short[] data, int bits, CancellationToken token)
        {
            byte[] encoded = new byte[data.Length];
            double A = 87.6;
 

            for (int i = 0; i < data.Length; i++)
            {
                if (i % 5000 == 0) token.ThrowIfCancellationRequested();

                double x = data[i] / (double)short.MaxValue;
                double absX = Math.Abs(x);
                double y = 0;

                if (absX < (1.0 / A)) y = (A * absX) / (1.0 + Math.Log(A));
                else y = (1.0 + Math.Log(A * absX)) / (1.0 + Math.Log(A));

                y = Math.Sign(x) * y;

                int quantized = (int)(((y + 1.0) / 2.0) * 255);
                encoded[i] = (byte)Math.Clamp(quantized, 0, 255);
            }

            return encoded;
        }

        private short[] DecompressNonlinearQuantization(byte[] data, int bits)
        {
            short[] decoded = new short[data.Length];
            double A = 87.6;

            for (int i = 0; i < data.Length; i++)
            {
                double y = ((data[i] / 255.0) * 2.0) - 1.0;
                double absY = Math.Abs(y);
                double x = 0;

                if (absY < (1.0 / (1.0 + Math.Log(A)))) x = absY * (1.0 + Math.Log(A)) / A;
                else x = Math.Exp(absY * (1.0 + Math.Log(A)) - 1.0) / A;

                x = Math.Sign(y) * x;

                decoded[i] = (short)Math.Clamp(x * short.MaxValue, short.MinValue, short.MaxValue);
            }
            return decoded;
        }
    }
}