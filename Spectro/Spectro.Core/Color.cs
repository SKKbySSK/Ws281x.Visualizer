using System;

namespace Spectro.Core
{
    public struct Color
    {
        private Color(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }
        
        public static Color FromARGB(byte a, byte r, byte g, byte b)
        {
            return new Color(a, r, g, b);
        }
        
        public static Color FromRGB(byte r, byte g, byte b)
        {
            return new Color(byte.MaxValue, r, g, b);
        }

        public static Color FromValueRGB(int color)
        {
            int r = (color & 0xff0000) >> 16;
            int g = (color & 0x00ff00) >> 8;
            int b = (color & 0x0000ff);

            return FromRGB((byte)r, (byte)g, (byte)b);
        }

        public static Color FromValueRGBA(long color)
        {
            long r = (color & 0xff000000) >> 32;
            long g = (color & 0x00ff0000) >> 16;
            long b = (color & 0x0000ff00) >> 8;
            long a = (color & 0x000000ff);

            return FromARGB((byte)a, (byte)r, (byte)g, (byte)b);
        }
        
        public byte A;
        public byte R;
        public byte G;
        public byte B;
    }
}
