using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Spectro.Core
{
    [StructLayout(LayoutKind.Explicit)]
    public struct UnionBuffer
    {
        [FieldOffset(0)]
        public byte[] Bytes;

        [FieldOffset(0)]
        public float[] Floats;
    }

    public class FillEventArgs
    {
        public byte[] Buffer;

        public int Offset;
        
        public int Count;

        public FillEventArgs(byte[] buffer, int offset, int count)
        {
            Buffer = buffer;
            Offset = offset;
            Count = count;
        }
    }
    
    public interface IAudioInput : IAudioSource
    {
        event EventHandler<FillEventArgs> Filled;

        event EventHandler Overflow;

        TimeSpan SoftwareLatency { get; }
    }
}
