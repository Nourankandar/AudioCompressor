using System;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace AudioCompressor.logic
{
    public class CompressionEngine
    {
        //public void CompressAudio(string inputWavPath, string outputCustomBinPath, string algorithm, int quantizationBits, CancellationToken token)
        //{
        //    if (!File.Exists(inputWavPath))
        //        throw new FileNotFoundException("Input audio file not found.");

        //    // 1. Read pure audio samples using NAudio
        //    using var reader = new AudioFileReader(inputWavPath);
        //    float[] floatBuffer = new float[reader.Length / 4];
        //    int samplesRead = reader.Read(floatBuffer, 0, floatBuffer.Length);

        //    short[] pcmData = new short[samplesRead];
        //    for (int i = 0; i < samplesRead; i++)
        //    {
        //        // Check for cancellation during read for massive files
        //        if (i % 10000 == 0) token.ThrowIfCancellationRequested();
        //        pcmData[i] = (short)(floatBuffer[i] * short.MaxValue);
        //    }

        //    // 2. Route to specific algorithm
        //    byte[] compressedData;
        //    switch (algorithm.ToUpper())
        //    {
        //        case "DPCM":
        //            compressedData = ExecuteDPCM(pcmData, token);
        //            break;
        //        case "DELTA MODULATION":
        //            compressedData = ExecuteDeltaModulation(pcmData, token);
        //            break;
        //        case "NONLINEAR QUANTIZATION":
        //            compressedData = ExecuteNonlinearQuantization(pcmData, quantizationBits, token);
        //            break;
        //        default:
        //            throw new ArgumentException("Unknown algorithm selected.");
        //    }

        //    try
        //    {

        //    token.ThrowIfCancellationRequested();

        //    }catch (Exception ex)
        //    {
        //        // Handle cancellation gracefully (e.g., delete partial file, log, etc.)
        //        if (File.Exists(outputCustomBinPath))
        //            File.Delete(outputCustomBinPath);
        //        //throw; // Re-throw to let the caller know it was cancelled
        //    }

        //    // 3. Save compressed data with custom metadata headers
        //    using var fs = new FileStream(outputCustomBinPath, FileMode.Create);
        //    using var bw = new BinaryWriter(fs);
        //    bw.Write(reader.WaveFormat.SampleRate);
        //    bw.Write(reader.WaveFormat.Channels);
        //    bw.Write(compressedData.Length);
        //    bw.Write(compressedData);
        //}



        public void CompressAudio(string inputWavPath, string outputCustomBinPath, string algorithm, int quantizationBits, CancellationToken token)
        {
            if (!File.Exists(inputWavPath))
                throw new FileNotFoundException("Input audio file not found.");

            try
            {
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

                // Final cancellation check before saving
                token.ThrowIfCancellationRequested();

                // 3. Save compressed data with custom metadata headers
                using var fs = new FileStream(outputCustomBinPath, FileMode.Create);
                using var bw = new BinaryWriter(fs);
                bw.Write(reader.WaveFormat.SampleRate);
                bw.Write(reader.WaveFormat.Channels);
                bw.Write(compressedData.Length);
                bw.Write(compressedData);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation gracefully: clean up the partial file if it exists
                if (File.Exists(outputCustomBinPath))
                    File.Delete(outputCustomBinPath);

                // MUST re-throw! This allows the catch (OperationCanceledException) block 
                // in ClickHelper.cs to run, which safely resets the progress bar and shows the warning.
                throw;
            }
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