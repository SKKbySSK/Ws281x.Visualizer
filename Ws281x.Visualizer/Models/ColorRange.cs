using System;
using System.Collections.Generic;
using System.Text;
using Range = Spectro.Core.Range;

namespace Ws281x.Visualizer.Models
{
    public class ColorRange
    {
        public Range Frequency { get; set; }

        public LedColor? Overlay { get; set; }

        public LedColor? PeakOverlay { get; set; }

        public double Minimum { get; set; } = -90;

        public double Maximum { get; set; } = -70;

        public double LedFrom { get; set; }

        public double LedTo { get; set; }
    }
}
