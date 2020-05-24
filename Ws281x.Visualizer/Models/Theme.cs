using System;
using System.Collections.Generic;
using System.Text;

namespace Ws281x.Visualizer.Models
{
    public class Theme
    {
        public string Name { get; set; }

        public List<ColorRange> ColorRanges { get; } = new List<ColorRange>();
    }
}
