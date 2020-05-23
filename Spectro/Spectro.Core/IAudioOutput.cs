using System;

namespace Spectro.Core
{
    public class UnderflowEventArgs : EventArgs
    {
        public UnderflowEventArgs(int? size)
        {
            Size = size;
        }

        public int? Size { get; }
    }
    
    public interface IAudioOutput
    {
        event EventHandler UnderflowTimedOut; 
        
        event EventHandler<UnderflowEventArgs> Underflow;

        AudioFormat Format { get; }

        void Write(byte[] buffer, int offset, int count);
        
        TimeSpan SoftwareLatency { get; }
    }
}
