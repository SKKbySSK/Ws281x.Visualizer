using System;
using System.IO;

namespace Spectro.Core
{
    public class WavePassthroughOutput : IAudioOutput
    {
        public WavePassthroughOutput(Stream stream = null)
        {
            Stream = stream;
        }

        public event EventHandler UnderflowTimedOut;
        
        public event EventHandler<UnderflowEventArgs> Underflow;
        
        public AudioFormat Format { get; }

        public Stream Stream { get; private set; }
        
        public void SetStream(Stream stream)
        {
            Stream = stream;
        }
        
        public void Write(byte[] buffer, int offset, int count)
        {
            Stream?.Write(buffer, offset, count);
        }

        public TimeSpan SoftwareLatency { get; } = TimeSpan.Zero;
    }
}