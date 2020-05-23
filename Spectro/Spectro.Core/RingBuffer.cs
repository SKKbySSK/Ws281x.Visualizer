using System;
using System.Threading;

namespace Spectro.Core
{
    /// <summary>
    /// Circular Buffer implementation that acts as a sliding window. New items are overwritting oldest
    /// values. Readers can get access to the buffer by copying it to their own Thread Local Storage.
    /// This implementation has been tested for thread safety and support multiple concurrent readers/writters.
    /// Please talk to Bogdan before modifying this class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RingBuffer<T> where T : struct
    {
        private int _lock;
        private readonly uint _capacity;

        private volatile uint _head;
        private volatile uint _tail;
        private readonly T[] _buffer;
        private bool _isEmpty;

        private RingBuffer()
        {
        }

        public RingBuffer(uint capacity)
        {
            _capacity = capacity;
            _buffer = new T[_capacity];
            _head = _tail = 0;
            _lock = 0;
            _isEmpty = true;
        }

        /// <summary>
        /// Returns the number of items present in the ring buffer.
        /// </summary>
        /// <returns></returns>
        public uint GetLength()
        {
            if (_isEmpty)
                return 0;

            if (_tail < _head)
                return _capacity - _head + _tail + 1;

            return _tail - _head + 1;
        }

        public uint GetHead()
        {
            return _head;
        }

        public uint GetTail()
        {
            return _tail;
        }

        /// <summary>
        /// Returns the maximum number of elements that the ring buffer can hold at
        /// any point in time.
        /// </summary>
        /// <returns></returns>
        public uint GetCapacity()
        {
            return _capacity;
        }

        /// <summary>
        /// Enqueues one item at the "tail" of the ring buffer. If the buffer is full
        /// the "head" element is overwritten.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        public void Enqueue(T item)
        {
            while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0) ;
            if (GetLength() == _capacity)
            {
                _head++;
                if (_head == _capacity)
                    _head = 0;
            }
            
            if (!_isEmpty)
                _tail++;
            if (_tail == _capacity)
                _tail = 0;
            _buffer[_tail] = item;

            _isEmpty = false;

            if (Interlocked.CompareExchange(ref _lock, 0, 1) != 1)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Clears the circular buffer discarding all items
        /// </summary>
        public void Clear()
        {
            while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0) ;
            _head = _tail = 0;
            _isEmpty = true;
            if (Interlocked.CompareExchange(ref _lock, 0, 1) != 1)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Enqueues an array of items at the "tail" of the ring buffer. If the buffer is full
        /// the elements at "head" will be overwritten.
        /// </summary>
        public void Enqueue(T[] items, int offset, int count)
        {
            if (count > _capacity)
                throw new InvalidOperationException(
                    "You are trying to add too many items. The buffer's capacity will be exceeded.");

            if (count == 0)
                return;

            while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0) ;

            uint availableSpace = _capacity - GetLength();

            // if _head will be overwritten, go ahead and advance it
            if (count - availableSpace > 0)
            {
                _head += ((uint)(count - availableSpace));
                _head = _head % _capacity; // todo: replace slow operation?
            }

            if (!_isEmpty)
                _tail++;

            _isEmpty = false;

            // make _tail point to where copying should start
            if (_tail >= _capacity)
                _tail = 0;

            // if _tail is close to the end of the array
            int howManyTillEnd = (_capacity - _tail) < count ? (int)(_capacity - _tail) : count;
            Array.Copy(items,offset,_buffer,_tail, howManyTillEnd);
            _tail += (uint)howManyTillEnd-1;

            if(_tail > _capacity)
                throw new ArithmeticException("There is a bug in the ring buffer!");

            if (howManyTillEnd < count)
            {   
                // prepare for another copy
                _tail++;
            }

            if (_tail == _capacity)
                _tail = 0;

            if (howManyTillEnd < count)
            {
                Array.Copy(items,howManyTillEnd,_buffer,_tail, count - howManyTillEnd);
                // _tail is protected from wrap around because we don't allow enqueuing of more items
                // than the _capacity of the buffer
                _tail += (uint) (count - howManyTillEnd -1);
            }
            
            if (Interlocked.CompareExchange(ref _lock, 0, 1) != 1)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Makes a copy of the ring buffer.
        /// </summary>
        /// <param name="destination">The destination array where the ring buffer's items will be copied.</param>
        /// <param name="head">The "head" of the ring buffer.</param>
        /// <param name="tail">The "tail" of the ring buffer.</param>
        public void Copy(Array destination, out uint head, out uint tail)
        {
            if (destination.Length < _capacity)
            {
                head = 0;
                tail = 0;
                return;
            }
            while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0) ;
            Array.Copy(_buffer, destination, _capacity);
            head = _head;
            tail = _tail;

            if (Interlocked.CompareExchange(ref _lock, 0, 1) != 1)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Discards "howMany" items from the head of the circular buffer and advances the head
        /// </summary>
        /// <param name="howMany">How many items to discard.</param>
        /// <returns></returns>
        public uint Discard(int howMany)
        {
            if (_isEmpty)
                return 0;

            while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0) ;
            uint available = GetLength();
            uint howManyToDiscard;

            if( howMany >= available )
            {
                howManyToDiscard = available;
                _isEmpty = true;
            }
            else
            {
                howManyToDiscard = (uint)howMany;
            }

            if (_tail < _head)
            {
                uint howManyTillEnd = (_capacity - _head) > howManyToDiscard ? howManyToDiscard : (_capacity - _head);
                _head += howManyTillEnd;
                if (_head > _capacity)
                    throw new ArithmeticException("There is a bug in the ring buffer!");
                if (_head == _capacity)
                    _head = 0;
                if (howManyToDiscard> howManyTillEnd)
                {
                    _head += howManyToDiscard- howManyTillEnd;
                }
            }
            else
            {
                if (_tail - _head + 1 < howManyToDiscard)
                    throw new ArithmeticException("There is a bug in the ring buffer!");

                _head += howManyToDiscard;
            }


            if (_isEmpty)
                _head = _tail = 0;

            if (Interlocked.CompareExchange(ref _lock, 0, 1) != 1)
            {
                throw new InvalidOperationException();
            }

            return howManyToDiscard;
        }

        /// <summary>
        /// The method will dequeue as many items from the circular buffer as the Length of
        /// the destination array. If not enough items are available, the destination array will be filled
        /// with the available items.
        /// </summary>
        /// <param name="destination">The destination array in which items will be dequeued.</param>
        /// <returns>The number of items that were actually returned.</returns>
        public uint Dequeue(Array destination)
        {
            if (_isEmpty)
                return 0;

            while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0) ;
            uint available = GetLength();
            uint howManyToDequeue;

            // determine how many items we can actually dequeue
            if( destination.Length >= available )
            {
                howManyToDequeue = available;
                _isEmpty = true;
            }
            else
            {
                howManyToDequeue = (uint)destination.Length;
            }
            
            if( _tail < _head )
            {
                uint howManyTillEnd = _capacity - _head > howManyToDequeue ? howManyToDequeue : _capacity - _head;
                Array.Copy(_buffer, _head, destination, 0, howManyTillEnd);
                _head += howManyTillEnd;
                if( _head > _capacity )
                    throw new ArithmeticException("There is a bug in the ring buffer!");
                if (_head == _capacity)
                    _head = 0;
                if( howManyToDequeue > howManyTillEnd )
                {
                    Array.Copy(_buffer, _head, destination, howManyTillEnd, howManyToDequeue - howManyTillEnd);
                    _head += howManyToDequeue - howManyTillEnd;
                }
            }
            else
            {
                if( _tail - _head + 1 < howManyToDequeue )
                    throw new ArithmeticException("There is a bug in the ring buffer!");

                Array.Copy(_buffer,_head, destination, 0, howManyToDequeue);
                _head += howManyToDequeue;
            }

            if (_isEmpty)
                _head = _tail = 0;

            if (Interlocked.CompareExchange(ref _lock, 0, 1) != 1)
            {
                throw new InvalidOperationException();
            }

            return howManyToDequeue;
        }
    }
}