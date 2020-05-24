using rpi_ws281x;
using Spectro.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Ws281x.Visualizer.Models;
using Range = Spectro.Core.Range;

namespace Ws281x.Visualizer
{
    public class Ws281xVisualizingOutput : SimpleVisualizingOutput, IDisposable
    {
        private Controller _controller;
        private WS281x _device;
        private LedColor[] _lastColors;

        public Range BrightnessRange { get; set; } = new Range(50, 4000);

        public double Brightness { get; set; } = 1;

        public double PeakBrightness { get; set; } = 0.5;

        public Theme Theme { get; set; }

        public LedColor Color { get; set; } = new LedColor(1, 1, 1);

        public LedColor PeakColor { get; set; } = new LedColor(0.01, 0.01, 0.01);

        public double Gain { get; set; } = 0;

        public ILog Log { get; set; }

        public bool UsePeakDetection { get; set; } = true;

        public Ws281xVisualizingOutput(WS281x device, Controller controller)
        {
            _device = device;
            _controller = controller;
        }

        protected override void ThreadFinished()
        {
            base.ThreadFinished();
            _device.Dispose();
        }

        protected override void WriteLed(AnalysisResult result)
        {
            var theme = Theme;
            if (theme == null)
            {
                return;
            }

            var colors = new LedColor[_controller.LEDCount];
            foreach (var colorRange in theme.ColorRanges)
            {
                ConfigureColors(result, colors, colorRange);
            }

            for (var i = 0; i < colors.Length; i++)
            {
                var color = colors[i] * Brightness;
                color.Red = Limit(color.Red);
                color.Green = Limit(color.Green);
                color.Blue = Limit(color.Blue);
                colors[i] = color;
                var col = color.AsColor();
                _controller.SetLED(i, col);
            }

            var log = Log;
            if (log != null && _lastColors != null)
            {
                int index = 0;
                foreach (var colorPair in colors.Zip(_lastColors))
                {
                    if (colorPair.First != colorPair.Second)
                    {
                        var col = colorPair.First.AsColor();
                        log.Verbose($"[{index.ToString("000")}] R:{col.R} G:{col.G} B:{col.B}");
                    }
                    index++;
                }
            }

            _lastColors = colors;
            _device.Render();
        }

        private double ConfigureBrightness(AnalysisResult result)
        {
            int offsetIndex = result.GetIndex(BrightnessRange.From);
            int endIndex = result.GetIndex(BrightnessRange.To);
            var dbfs = result.GetDBFS(offsetIndex, endIndex);

            var averageDbfs = 0.0;
            foreach (var value in dbfs)
            {
                averageDbfs += value;
            }

            averageDbfs /= dbfs.Length;

            double minimum = -90;
            double maximum = -70;
            var linear = (averageDbfs - minimum) / (maximum - minimum);
            if (!double.IsNormal(linear))
            {
                linear = 0;
            }

            linear = Math.Max(0, Math.Min(linear, 1));
            var brightness = Limit(Math.Log2(1 + linear));
            brightness = 1;
            return brightness;
        }

        private void ConfigureColors(AnalysisResult result, LedColor[] colors, ColorRange range)
        {
            double[] dbfs;
            double averageDbfs;
            double linear;
            int offsetIndex = result.GetIndex(range.Frequency.From);
            int endIndex = result.GetIndex(range.Frequency.To);
            dbfs = result.GetDBFS(offsetIndex, endIndex);

            averageDbfs = 0.0;
            foreach (var value in dbfs)
            {
                averageDbfs += value;
            }

            averageDbfs /= dbfs.Length;
            averageDbfs += Gain;

            linear = (averageDbfs - range.Minimum) / (range.Maximum - range.Minimum);
            if (!double.IsNormal(linear))
            {
                linear = 0;
            }

            linear = Math.Max(0, Math.Min(linear, 1));

            var maxFreq = range.Frequency.To;
            int ledOffset = (int)(Math.Round(range.LedFrom * _controller.LEDCount));
            int ledCount = (int)(Math.Round((range.LedTo - range.LedFrom) * _controller.LEDCount));

            if (ledOffset + ledCount >= colors.Length)
            {
                ledCount = colors.Length - ledOffset;
            }

            int ledEndIndex = ledOffset + ledCount - 1;

            var overlay = range.Overlay ?? new LedColor(1, 1, 1);
            var peakOverlay = range.PeakOverlay ?? new LedColor(1, 1, 1);

            for (int i = ledOffset; i <= ledEndIndex; i++)
            {
                colors[i] += Color * overlay * linear;
            }

            if (UsePeakDetection)
            {
                var peaks = result.GetPeaks(offsetIndex, endIndex);
                double peakDbfs;
                foreach (var peak in peaks)
                {
                    peakDbfs = dbfs[peak.Key] + Gain;

                    linear = (peakDbfs - range.Minimum) / (range.Maximum - range.Minimum);
                    if (!double.IsNormal(linear))
                    {
                        linear = 0;
                    }

                    linear = Math.Max(0, Math.Min(linear, 1));

                    var index = GetLedIndex(maxFreq, range.Frequency.From, peak.Value, ledOffset, ledCount);
                    colors[index] += PeakColor * peakOverlay * linear * PeakBrightness;
                }
            }
        }

        private int GetLedIndex(double maxFrequency, double offsetFrequency, double frequency, int offset, int length)
        {
            var ratio = (frequency - offsetFrequency) / (maxFrequency - offsetFrequency);
            var index = (int)Math.Round(ratio * (length - 1)) + offset;
            return Math.Min(index, length - 1 + offset);
        }

        private byte ToByte(double value)
        {
            return (byte)(Limit(value) * byte.MaxValue);
        }

        private double Limit(double value, double max = 1, double min = 0)
        {
            return Math.Max(Math.Min(value, max), min);
        }

        public void Dispose()
        {
            _controller = null;
            _device?.Dispose();
            _device = null;
        }
    }
}
