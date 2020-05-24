using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Ws281x.Visualizer.Models
{
    public struct LedColor
    {
        public LedColor(double red, double green, double blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        public double Red { get; set; }

        public double Green { get; set; }

        public double Blue { get; set; }

        public Color AsColor()
        {
            return Color.FromArgb((int)(Red * byte.MaxValue), (int)(Green * byte.MaxValue), (int)(Blue * byte.MaxValue));
        }

        public override bool Equals(object obj)
        {
            return obj is LedColor color &&
                   Red == color.Red &&
                   Green == color.Green &&
                   Blue == color.Blue;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Red, Green, Blue);
        }

        public static LedColor operator *(LedColor color, double value)
        {
            return new LedColor()
            {
                Red = color.Red * value,
                Green = color.Green * value,
                Blue = color.Blue * value
            };
        }

        public static LedColor operator +(LedColor x, LedColor y)
        {
            return new LedColor()
            {
                Red = x.Red + y.Red,
                Green = x.Green + y.Green,
                Blue = x.Blue + y.Blue
            };
        }

        public static LedColor operator -(LedColor x, LedColor y)
        {
            return new LedColor()
            {
                Red = x.Red - y.Red,
                Green = x.Green - y.Green,
                Blue = x.Blue - y.Blue
            };
        }

        public static LedColor operator *(LedColor x, LedColor y)
        {
            return new LedColor()
            {
                Red = x.Red * y.Red,
                Green = x.Green * y.Green,
                Blue = x.Blue * y.Blue
            };
        }

        public static bool operator ==(LedColor x, LedColor y)
        {
            return x.Red == y.Red && x.Green == y.Green && x.Blue == y.Blue;
        }

        public static bool operator !=(LedColor x, LedColor y)
        {
            return !(x == y);
        }
    }
}
