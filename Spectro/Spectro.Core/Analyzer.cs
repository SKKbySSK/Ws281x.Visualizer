using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using MathNet.Numerics;

namespace Spectro.Core
{
    public class AnalysisResult
    {
        public AnalysisResult(Analyzer analyzer, Complex[] result)
        {
            Result = result;
            Size = analyzer.Size;
            SampleRate = analyzer.SampleRate;
            FrequencyResolution = analyzer.FrequencyResolution;
        }

        public Complex[] Result { get; }
        
        public int Size { get; }
        
        public int SampleRate { get; }
        
        public double FrequencyResolution { get; }

        public unsafe double[] GetPowerSpectrum(int offset, int endIndex)
        {
            var spectrum = new double[endIndex + 1 - offset];
            if (endIndex > Size || offset > endIndex)
            {
                throw new IndexOutOfRangeException();
            }
            
            fixed (Complex* res = Result)
            {
                for (int i = offset; endIndex >= i; i++)
                {
                    spectrum[i - offset] = res[i].Magnitude;
                }
            }

            return spectrum;
        }

        public double[] GetPowerSpectrum(int offset = 0)
        {
            return GetPowerSpectrum(offset, Size - 1);
        }

        /// <summary>
        /// Calculate dBFS (Decibels relative to full scale)
        /// </summary>
        /// <returns></returns>
        public double[] GetDBFS(int offset, int endIndex)
        {
            var power = GetPowerSpectrum(offset, endIndex);

            for (int i = 0; power.Length > i; i++)
            {
                // https://www.kvraudio.com/forum/viewtopic.php?t=276092
                power[i] = 20 * Math.Log10(2 * power[i] / Size);
            }

            return power;
        }

        /// <summary>
        /// Calculate dBFS (Decibels relative to full scale)
        /// </summary>
        /// <returns></returns>
        public double[] GetDBFS(int offset)
        {
            return GetDBFS(offset, Size - 1);
        }

        public double[] GetDB(int offset, int endIndex)
        {
            var power = GetPowerSpectrum(offset, endIndex);

            for (int i = 0; power.Length > i; i++)
            {
                power[i] = 20 * Math.Log10(power[i]);
            }

            return power;
        }

        public List<int> GetPeakIndicies(int offset, int endIndex, int stride = 2, double minimumThreshold = -100)
        {
            var indicies = new List<int>();
            var power = GetDBFS(offset, endIndex);
            
            int slimCount;
            int slimIndex = 0;
            if (power.Length % stride == 0)
            {
                slimCount = power.Length / stride;
            }
            else
            {
                slimCount = power.Length / stride + 1;
            }

            var slimPowerIndex = new int[slimCount];
            var slimPower = new double[slimCount];
            
            double av = 0;
            for (var i = 0; i < power.Length; i += stride)
            {
                int sum = Math.Min(power.Length - i, stride);
                slimPowerIndex[slimIndex] = i;
                for (int j = 0; j < sum; j++)
                {
                    av += power[i + j];
                }

                av /= sum;
                slimPower[slimIndex++] = av;
                av = 0;
            }

            for (var i = 0; i < slimPower.Length; i++)
            {
                av += slimPower[i];
            }

            av /= slimPower.Length;

            double value;
            for (var i = 0; i < slimPower.Length; i++)
            {
                value = slimPower[i];
                if (value > av && value >= minimumThreshold)
                {
                    indicies.Add(slimPowerIndex[i]);
                }
            }
            
            return indicies;
        }

        public Dictionary<int, double> GetPeaks(int offset, int endIndex, int stride = 2,
            double minimumThreshold = -100)
        {
            var peaks = GetPeakIndicies(offset, endIndex, stride, minimumThreshold);
            var dict = new Dictionary<int, double>();

            foreach (var peakIndex in peaks)
            {
                dict[peakIndex] = GetFrequency(offset + peakIndex);
            }

            return dict;
        }

        public double[] GetDB(int offset)
        {
            return GetDB(offset, Size - 1);
        }

        public int GetIndex(double freq)
        {
            var index = (int)Math.Round(freq / FrequencyResolution);
            return Math.Min(index, Size);
        }

        public double GetFrequency(int index)
        {
            return index * FrequencyResolution;
        }
    }
    
    public class Analyzer
    {
        private readonly double[] window;
        
        public Analyzer(int size, int sampleRate)
        {
            Size = size;
            SampleRate = sampleRate;
            FrequencyResolution = sampleRate / (double)size;
            window = Window.Blackman(size);
        }
        
        public int Size { get; }
        
        public int SampleRate { get; }
        
        public double FrequencyResolution { get; }
        
        public AnalysisResult Fft(byte[] buffer, int offset, int bits)
        {
            int stride = bits / 8;
            var dBuffer = new double[buffer.Length / stride];
            for (var i = 0; i < buffer.Length; i += stride)
            {
                switch (bits)
                {
                    case 16:
                        var value = BitConverter.ToInt16(buffer, i);
                        dBuffer[i / stride] = value / (double) short.MaxValue;
                        break;
                    default:
                        throw new NotSupportedException($"{bits} bits sample is not supported");
                }
            }
            
            return Fft(dBuffer, offset / stride);
        }

        public unsafe AnalysisResult Fft(double[] buffer, int offset)
        {
            if (buffer.Length - offset < Size)
            {
                throw new ArgumentException();
            }

            int size = Size;
            var fftResult = new Complex[size];
            fixed (double* buf = buffer)
            fixed (double* win = window)
            fixed (Complex* res = fftResult)
            {
                for (int i = offset; size > i; i++)
                {
                    res[i] = new Complex(buf[i] * win[i], 0);
                }
            }

            MathNet.Numerics.IntegralTransforms.Fourier.Forward(fftResult);
            return new AnalysisResult(this, fftResult);
        }

        public int GetIndex(double freq)
        {
            var index = (int)Math.Round(freq / FrequencyResolution);
            return Math.Min(index, Size);
        }

        public double GetFrequency(int index)
        {
            return index * FrequencyResolution;
        }
    }
}
