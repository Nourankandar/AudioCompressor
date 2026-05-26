using System;
using System.IO;

namespace AudioCompressor.logic
{
    public class CompressionEngine
    {
        // 1. التابع الرئيسي لضغط الصوت بناءً على الخوارزمية والإعدادات المختارة (الطلب 6)
        public byte[] CompressAudio(string inputPath, string algorithm, int sampleRate, int quantizationBits)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Input audio file not found.");

            // قراءة البيانات الخام للملف
            byte[] rawData = File.ReadAllBytes(inputPath);
            
            switch (algorithm.ToUpper())
            {
                case "DPCM":
                    return ExecuteDPCM(rawData);

                case "DELTA MODULATION":
                    return ExecuteDeltaModulation(rawData);

                case "NONLINEAR QUANTIZATION":
                    // التكميم غير الخطي يعتمد مباشرة على عدد البتات الممرر من الواجهة
                    return ExecuteNonlinearQuantization(rawData, quantizationBits);

                default:
                    return rawData; 
            }
        }

        // 2. تابع فك ضغط الصوت لاسترجاع الإشارة الصوتية الأصلية
        public byte[] DecompressAudio(byte[] compressedData, string algorithm, int quantizationBits)
        {
            switch (algorithm.ToUpper())
            {
                case "DPCM":
                    return DecompressDPCM(compressedData);

                case "DELTA MODULATION":
                    return DecompressDeltaModulation(compressedData);

                case "NONLINEAR QUANTIZATION":
                    return DecompressNonlinearQuantization(compressedData, quantizationBits);

                default:
                    return compressedData;
            }
        }

        // ----------------------------------------------------
        // الخوارزمية الأولى: Differential Pulse Code Modulation
        // ----------------------------------------------------
        private byte[] ExecuteDPCM(byte[] data)
        {
            if (data.Length == 0) return data;
            byte[] encoded = new byte[data.Length];
            
            // العينة الأولى تنزل كما هي كمرجع
            encoded[0] = data[0]; 
            
            for (int i = 1; i < data.Length; i++)
            {
                // حساب الفرق بين العينة الحالية والسابقة
                int diff = data[i] - data[i - 1];
                
                // تحويل الفرق إلى byte (إضافة 128 لتفادي القيم السالبة في مصفوفة الـ bytes)
                encoded[i] = (byte)(diff + 128);
            }
            return encoded;
        }

        private byte[] DecompressDPCM(byte[] data)
        {
            if (data.Length == 0) return data;
            byte[] decoded = new byte[data.Length];
            
            decoded[0] = data[0];
            for (int i = 1; i < data.Length; i++)
            {
                // استرجاع الفرق الحقيقي بطرح الـ 128
                int diff = data[i] - 128;
                
                // العينة الحالية = العينة السابقة + الفرق
                decoded[i] = (byte)(decoded[i - 1] + diff);
            }
            return decoded;
        }

        // ----------------------------------------------------
        // الخوارزمية الثانية: Delta Modulation (1-bit processing)
        // ----------------------------------------------------
        private byte[] ExecuteDeltaModulation(byte[] data)
        {
            if (data.Length == 0) return data;
            byte[] encoded = new byte[data.Length];
            byte stepSize = 4; // مقدار الخطوة الثابتة (Delta)
            byte predictedValue = data[0];
            
            encoded[0] = data[0];

            for (int i = 1; i < data.Length; i++)
            {
                if (data[i] >= predictedValue)
                {
                    encoded[i] = 1; // 1 تعني زيادة بمقدار الخطوة
                    predictedValue = (byte)Math.Min(255, predictedValue + stepSize);
                }
                else
                {
                    encoded[i] = 0; // 0 تعني نقصان بمقدار الخطوة
                    predictedValue = (byte)Math.Max(0, predictedValue - stepSize);
                }
            }
            return encoded;
        }

        private byte[] DecompressDeltaModulation(byte[] data)
        {
            if (data.Length == 0) return data;
            byte[] decoded = new byte[data.Length];
            byte stepSize = 4;
            
            decoded[0] = data[0];
            byte predictedValue = data[0];

            for (int i = 1; i < data.Length; i++)
            {
                if (data[i] == 1)
                {
                    predictedValue = (byte)Math.Min(255, predictedValue + stepSize);
                }
                else
                {
                    predictedValue = (byte)Math.Max(0, predictedValue - stepSize);
                }
                decoded[i] = predictedValue;
            }
            return decoded;
        }

        // ----------------------------------------------------
        // الخوارزمية الثالثة: Nonlinear Quantization (A-Law compression)
        // ----------------------------------------------------
        private byte[] ExecuteNonlinearQuantization(byte[] data, int bits)
        {
            byte[] encoded = new byte[data.Length];
            double A = 87.6; // المعامل العالمي القياسي لخوارزمية A-Law
            
            // حساب عدد مستويات التكميم بناءً على البتات المحددة من الواجهة (مثلاً 8 بت تعطي 256 مستوى)
            int levels = (int)Math.Pow(2, bits); 

            for (int i = 0; i < data.Length; i++)
            {
                // تحويل قيمة الـ byte إلى مجال بين -1.0 و 1.0 ل تطبيق اللوغاريتم
                double x = (data[i] - 128.0) / 128.0;
                double absX = Math.Abs(x);
                double y = 0;

                // تطبيق معادلة الضغط اللوغاريتمي غير الخطي لـ A-Law
                if (absX < (1.0 / A))
                {
                    y = (A * absX) / (1.0 + Math.Log(A));
                }
                else if (absX >= (1.0 / A) && absX <= 1.0)
                {
                    y = (1.0 + Math.Log(A * absX)) / (1.0 + Math.Log(A));
                }

                y = Math.Sign(x) * y;

                // إعادة التدريج (Quantization Mapping) للمستويات المطلوبة
                int quantized = (int)(((y + 1.0) / 2.0) * (levels - 1));
                encoded[i] = (byte)((quantized * 255) / (levels - 1));
            }
            return encoded;
        }

        private byte[] DecompressNonlinearQuantization(byte[] data, int bits)
        {
            byte[] decoded = new byte[data.Length];
            double A = 87.6;

            for (int i = 0; i < data.Length; i++)
            {
                // تحويل القيمة المشفرة إلى مجال -1.0 إلى 1.0
                double y = (data[i] - 128.0) / 128.0;
                double absY = Math.Abs(y);
                double x = 0;

                // عكس معادلة اللوغاريتم لاستعادة الإشارة الأصلية التقريبية
                if (absY < (1.0 / (1.0 + Math.Log(A))))
                {
                    x = absY * (1.0 + Math.Log(A)) / A;
                }
                else
                {
                    x = Math.Exp(absY * (1.0 + Math.Log(A)) - 1.0) / A;
                }

                x = Math.Sign(y) * x;
                decoded[i] = (byte)((x * 128.0) + 128.0);
            }
            return decoded;
        }
    }
}