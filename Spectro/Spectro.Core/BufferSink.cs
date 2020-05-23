using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Spectro.Core
{

    public class BufferSink<T>
    {
        private bool reachLast = false;

        public BufferSink(int size)
        {
            Size = size;
        }

        public BufferSink() : this(0)
        {
            
        }

        public int Size { get; set; }

        private List<Memory<T>> Buffers { get; } = new List<Memory<T>>();

        public int BufferedDataCount { get; private set; } = 0;

        public bool IsFilled => Size > 0 && reachLast || BufferedDataCount >= Size;

        public event EventHandler Filled;

        public void Push(T[] buffer, bool isLastBuffer)
        {
            Buffers.Add(buffer);
            BufferedDataCount += buffer.Length;
            reachLast = isLastBuffer;
            
            if (IsFilled)
            {
                Filled?.Invoke(this, EventArgs.Empty);
            }
        }

        public void PushCopied(T[] buffer, int offset, int count, bool isLastBuffer)
        {
            T[] copied = new T[count];
            Array.Copy(buffer, offset, copied, 0, count);

            Push(copied, isLastBuffer);
        }

        public T[] Pop(int size)
        {
            int len = Math.Min(size, BufferedDataCount);

            int read = 0, index = 0, offset = 0;
            Memory<T>? buffer = null;
            T[] data = new T[len];
            for (; len > read; read++)
            {
                if (buffer == null)
                {
                    buffer = Buffers[0];
                    Buffers.RemoveAt(0);
                }

                index = read - offset;
                if (index < buffer.Value.Length)
                {
                    data[read] = buffer.Value.Span[index];
                }
                else
                {
                    offset += buffer.Value.Length;
                    buffer = null;
                    read--;
                }
            }

            BufferedDataCount -= len;

            if (buffer != null)
            {
                int ind = len - offset;

                if (ind > 0)
                {
                    Buffers.Insert(0, buffer.Value.Slice(ind));
                }
            }

            return data;
        }

        public T[] Pop()
        {
            return Pop(Size);
        }

        public void Reset()
        {
            Buffers.Clear();
            reachLast = false;
        }
    }
}