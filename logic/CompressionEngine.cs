using System;
using System.IO;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AudioCompressor.logic
{
    public class CompressionEngine
    {

        public void CompressAudio(
            string inputWavPath,
            string outputCustomBinPath,
            string algorithm,
            int targetSampleRate,
            int quantizationBits,
            int stepSize,
            CancellationToken token)
        {
            if (!File.Exists(inputWavPath))
                throw new FileNotFoundException("Input audio file not found.");

            try
            {
                using var reader = new AudioFileReader(inputWavPath);

                int    originalSampleRate = reader.WaveFormat.SampleRate;
                int    channels           = reader.WaveFormat.Channels;
                float[] floatBuffer;
                int     samplesRead;

                if (targetSampleRate != originalSampleRate)
                {
                    var resampler = new WdlResamplingSampleProvider(reader, targetSampleRate);

                    double ratio          = (double)targetSampleRate / originalSampleRate;
                    int    estimatedCount = (int)(reader.Length / 4 * ratio) + channels * 2;

                    floatBuffer = new float[estimatedCount];
                    samplesRead = resampler.Read(floatBuffer, 0, estimatedCount);
                }
                else
                {
                    floatBuffer = new float[reader.Length / 4];
                    samplesRead = reader.Read(floatBuffer, 0, floatBuffer.Length);
                }

                short[] pcmData = new short[samplesRead];
                for (int i = 0; i < samplesRead; i++)
                {
                    if (i % 10000 == 0) token.ThrowIfCancellationRequested();
                    pcmData[i] = (short)(Math.Clamp(floatBuffer[i], -1f, 1f) * short.MaxValue);
                }

                byte[] compressedData;
                switch (algorithm.ToUpper())
                {
                    case "DPCM":
                        compressedData = ExecuteDPCM(pcmData, token);
                        break;
                    case "DELTA MODULATION":
                        compressedData = ExecuteDeltaModulation(pcmData, stepSize, token);
                        break;
                    case "NONLINEAR QUANTIZATION":
                        compressedData = ExecuteNonlinearQuantization(pcmData, quantizationBits, token);
                        break;
                    default:
                        throw new ArgumentException("Unknown algorithm selected.");
                }

                token.ThrowIfCancellationRequested();

                using var fs = new FileStream(outputCustomBinPath, FileMode.Create);
                using var bw = new BinaryWriter(fs);

                bw.Write(targetSampleRate);      
                bw.Write(channels);               
                bw.Write(quantizationBits);       
                bw.Write(stepSize);               
                bw.Write(compressedData.Length);  
                bw.Write(compressedData);         
            }
            catch (OperationCanceledException)
            {
                if (File.Exists(outputCustomBinPath))
                    File.Delete(outputCustomBinPath);
                throw;
            }
        }
        public void DecompressAudio(
            string compressedBinPath,
            string outputWavPath,
            string algorithm)
        {
            if (!File.Exists(compressedBinPath))
                throw new FileNotFoundException("Compressed .bin file not found.");

            using var fs = new FileStream(compressedBinPath, FileMode.Open);
            using var br = new BinaryReader(fs);

            int sampleRate       = br.ReadInt32();  
            int channels         = br.ReadInt32();
            int quantizationBits = br.ReadInt32();
            int stepSize         = br.ReadInt32();
            int dataLength       = br.ReadInt32();
            byte[] compressedData = br.ReadBytes(dataLength);

            short[] decompressedPcm;
            switch (algorithm.ToUpper())
            {
                case "DPCM":
                    decompressedPcm = DecompressDPCM(compressedData);
                    break;
                case "DELTA MODULATION":
                    decompressedPcm = DecompressDeltaModulation(compressedData, stepSize);
                    break;
                case "NONLINEAR QUANTIZATION":
                    decompressedPcm = DecompressNonlinearQuantization(compressedData, quantizationBits);
                    break;
                default:
                    throw new ArgumentException("Unknown algorithm selected.");
            }

            // كتابة ملف WAV بنفس الـ SR المحفوظ
            var format = new WaveFormat(sampleRate, 16, channels);
            using var writer = new WaveFileWriter(outputWavPath, format);

            byte[] finalWavBytes = new byte[decompressedPcm.Length * 2];
            Buffer.BlockCopy(decompressedPcm, 0, finalWavBytes, 0, finalWavBytes.Length);
            writer.Write(finalWavBytes, 0, finalWavBytes.Length);
        }

        private byte[] ExecuteDPCM(short[] data, CancellationToken token)
        {
            if (data.Length == 0) return Array.Empty<byte>();

            byte[] encoded = new byte[data.Length];
            encoded[0] = (byte)((data[0] >> 8) + 128);

            for (int i = 1; i < data.Length; i++)
            {
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

        private byte[] ExecuteDeltaModulation(short[] data, int stepSize, CancellationToken token)
        {
            if (data.Length == 0) return Array.Empty<byte>();

            int byteCount = (int)Math.Ceiling(data.Length / 8.0);
            byte[] encoded = new byte[byteCount];
            short step = (short)Math.Clamp(stepSize, 1, short.MaxValue);
            short predictedValue = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (i % 5000 == 0) token.ThrowIfCancellationRequested();

                int byteIndex = i / 8;
                int bitIndex  = i % 8;

                if (data[i] >= predictedValue)
                {
                    encoded[byteIndex] |= (byte)(1 << (7 - bitIndex));
                    predictedValue = (short)Math.Min(short.MaxValue, predictedValue + step);
                }
                else
                {
                    predictedValue = (short)Math.Max(short.MinValue, predictedValue - step);
                }
            }
            return encoded;
        }

        private short[] DecompressDeltaModulation(byte[] data, int stepSize)
        {
            int sampleCount = data.Length * 8;
            short[] decoded = new short[sampleCount];

            short step = (short)Math.Clamp(stepSize, 1, short.MaxValue);
            short predictedValue = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                int byteIndex = i / 8;
                int bitIndex  = i % 8;

                bool isOne = (data[byteIndex] & (1 << (7 - bitIndex))) != 0;

                if (isOne)
                    predictedValue = (short)Math.Min(short.MaxValue, predictedValue + step);
                else
                    predictedValue = (short)Math.Max(short.MinValue, predictedValue - step);

                decoded[i] = predictedValue;
            }
            return decoded;
        }

        private byte[] ExecuteNonlinearQuantization(short[] data, int bits, CancellationToken token)
        {
            if (data.Length == 0) return Array.Empty<byte>();

            double A = 87.6;
            int levels = (int)Math.Pow(2, Math.Clamp(bits, 2, 16));

            int totalBits = data.Length * bits;
            int byteCount = (totalBits + 7) / 8;
            byte[] encoded = new byte[byteCount];

            int currentBitIndex = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (i % 5000 == 0) token.ThrowIfCancellationRequested();

                double x = data[i] / (double)short.MaxValue;
                double absX = Math.Abs(x);
                double y;

                if (absX < (1.0 / A))
                    y = (A * absX) / (1.0 + Math.Log(A));
                else
                    y = (1.0 + Math.Log(A * absX)) / (1.0 + Math.Log(A));

                y = Math.Sign(x) * y;

                int quantized = (int)(((y + 1.0) / 2.0) * (levels - 1));
                quantized = Math.Clamp(quantized, 0, levels - 1);

                for (int b = 0; b < bits; b++)
                {
                    int bitVal = (quantized >> (bits - 1 - b)) & 1; 
                    
                    if (bitVal == 1)
                    {
                        int byteIdx = currentBitIndex / 8;
                        int bitIdx = currentBitIndex % 8;
                        encoded[byteIdx] |= (byte)(1 << (7 - bitIdx)); 
                    }
                    currentBitIndex++;
                }
            }
            return encoded;
        }

        private short[] DecompressNonlinearQuantization(byte[] data, int bits)
        {
            if (data.Length == 0) return Array.Empty<short>();

            bits = Math.Clamp(bits, 2, 16);
            double A = 87.6;
            int levels = (int)Math.Pow(2, bits);

            int totalBits = data.Length * 8;
            int sampleCount = totalBits / bits;
            
            short[] decoded = new short[sampleCount];
            int currentBitIndex = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                int quantized = 0;

                for (int b = 0; b < bits; b++)
                {
                    int byteIdx = currentBitIndex / 8;
                    int bitIdx = currentBitIndex % 8;

                    if (byteIdx < data.Length)
                    {
                        int bitVal = (data[byteIdx] >> (7 - bitIdx)) & 1;
                        quantized = (quantized << 1) | bitVal;
                    }
                    currentBitIndex++;
                }

                double y = ((quantized / (double)(levels - 1)) * 2.0) - 1.0;
                double absY = Math.Abs(y);
                double x;

                if (absY < (1.0 / (1.0 + Math.Log(A))))
                    x = absY * (1.0 + Math.Log(A)) / A;
                else
                    x = Math.Exp(absY * (1.0 + Math.Log(A)) - 1.0) / A;

                x = Math.Sign(y) * x;

                decoded[i] = (short)Math.Clamp(x * short.MaxValue, short.MinValue, short.MaxValue);
            }
            return decoded;
        }
        private static float[] ReadAllFloatSamples(ISampleProvider provider, CancellationToken token)
        {
            var samples = new List<float>();
            float[] buffer = new float[65536];
            int read;

            while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    if (i % 10000 == 0) token.ThrowIfCancellationRequested();
                    samples.Add(buffer[i]);
                }
            }

            return samples.ToArray();
        }
    }
        
    }
    
