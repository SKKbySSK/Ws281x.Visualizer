using System;
using SoundIOSharp;
using Spectro.Core;

namespace Spectro.Cross.Soundio
{
    public static class Soundio
    {
        public static AudioFormat ToManagedFormat(SoundIOFormat format, int sampleRate, int channels)
        {
            if (format == SoundIODevice.S16NE)
            {
                return new AudioFormat(sampleRate, channels, 16, true);
            }
            else if (format == SoundIODevice.U16NE)
            {
                return new AudioFormat(sampleRate, channels, 16, false);
            }
            else if (format == SoundIODevice.S24NE)
            {
                return new AudioFormat(sampleRate, channels, 24, true);
            }
            else if (format == SoundIODevice.U24NE)
            {
                return new AudioFormat(sampleRate, channels, 24, false);
            }
            else if (format == SoundIODevice.Float32NE)
            {
                return new AudioFormat(sampleRate, channels, 32, true);
            }
            else
            {
                return null;
            }
        }

        public static SoundIOFormat? ToSoundioFormat(AudioFormat format)
        {
            if (format.BitDepth == 16)
            {
                return format.Signed ? SoundIODevice.S16NE : SoundIODevice.U16NE;
            }
            else if (format.BitDepth == 24)
            {
                return format.Signed ? SoundIODevice.S24NE : SoundIODevice.U24NE;
            }
            else if (format.BitDepth == 32)
            {
                return format.Signed ? SoundIODevice.Float32NE : new SoundIOFormat?();
            }
            else
            {
                return null;
            }
        }
    }
}
