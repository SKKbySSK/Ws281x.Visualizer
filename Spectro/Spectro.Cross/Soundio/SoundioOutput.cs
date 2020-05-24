using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SoundIOSharp;
using Spectro.Core;

namespace Spectro.Cross.Soundio
{
    public class OutputInitializationException : Exception
    {
        public OutputInitializationException(string message) : base(message)
        {
        }
        
        public OutputInitializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class SoundioOutput : IAudioOutput, IDisposable
    {
        private SoundIO _api = new SoundIO();
        private SoundIOOutStream _outstream;
        private RingBuffer<byte> _ringBuffer;
        private TimeSpan _bufferDuration;

        public SoundioOutput(SoundIOBackend? backend = null, TimeSpan? bufferDuration = null)
        {
            _bufferDuration = bufferDuration ?? TimeSpan.FromSeconds(30);
            if (backend.HasValue)
            {
                _api.ConnectBackend(backend.Value);
            }
            else
            {
                _api.Connect();
            }
            _api.FlushEvents();
            
            for (int i = 0; _api.OutputDeviceCount > i; i++)
            {
                var device = _api.GetOutputDevice(i);
                if (i == _api.DefaultOutputDeviceIndex)
                {
                    DefaultDevice = device;
                }

                Devices.Add(device);
            }
        }

        public SoundIOBackend Backend => _api.CurrentBackend;

        public event EventHandler UnderflowTimedOut;
        
        public event EventHandler<UnderflowEventArgs> Underflow;

        public event EventHandler UnrecoverableError;

        public TimeSpan DesiredLatency { get; set; } = TimeSpan.FromMilliseconds(30);

        public SoundIODevice Device { get; private set; }

        public List<SoundIODevice> Devices { get; } = new List<SoundIODevice>();

        public SoundIODevice DefaultDevice { get; }
        
        public AudioFormat Format { get; private set; }

        public int UnderflowRetryCount { get; set; } = 1;
        
        public void Write(byte[] buffer, int offset, int count)
        {
            _ringBuffer.Enqueue(buffer, offset, count);
        }

        public TimeSpan SoftwareLatency { get; private set; }

        public void Initialize(SoundIODevice device, AudioFormat format)
        {
            try
            {
                Device = device;
                initInternal(format);
            }
            catch (Exception)
            {
                Device = null;
                throw;
            }
        }

        public void Start()
        {
            if (_outstream == null)
            {
                throw new Exception("SoundioOutput is not initialized");
            }
            
            _outstream.Start();
        }

        public void Stop()
        {
            if (_outstream == null)
            {
                throw new Exception("SoundioOutput is not initialized");
            }

            _outstream.Dispose();
            _outstream = null;
        }

        private void initInternal(AudioFormat format)
        {            
            if (Device == null)
            {
                throw new Exception("No device is selected");
            }

            if (Device.ProbeError != 0)
            {
                throw new OutputInitializationException($"Probe Error : {Device.ProbeError}");
            }
            
            _outstream = Device.CreateOutStream();
            _outstream.WriteCallback = (min, max) => write_callback(_outstream, min, max);
            _outstream.UnderflowCallback = () => Underflow?.Invoke(this, new UnderflowEventArgs(null));
            _outstream.ErrorCallback = () => UnrecoverableError?.Invoke(this, EventArgs.Empty);
            _outstream.SampleRate = format.SampleRate;
            _outstream.SoftwareLatency = DesiredLatency.TotalSeconds;

            var soundioFormat = Soundio.ToSoundioFormat(format);
            _outstream.Format = soundioFormat ?? SoundIOFormat.Invalid;
            
            if (_outstream.LayoutErrorMessage != null)
            {
                var msg = _outstream.LayoutErrorMessage;
                Console.WriteLine($"Channel Layout Error : {msg}");
            }
            
            _outstream.Open();
            _api.FlushEvents();
            
            Format = Soundio.ToManagedFormat(_outstream.Format, _outstream.SampleRate, _outstream.Layout.ChannelCount);
            SoftwareLatency = TimeSpan.FromSeconds(_outstream.SoftwareLatency);

            var bytesPerSample = _outstream.BytesPerSample;
            var capacity = Format.SampleRate * Format.Channels * bytesPerSample *
                           _bufferDuration.TotalSeconds;
            _ringBuffer = new RingBuffer<byte>((uint)capacity);
        }

        unsafe void write_callback(SoundIOOutStream outstream, int frame_count_min, int frame_count_max)
        {
            int frame_count = frame_count_max;
            var results = outstream.BeginWrite(ref frame_count);

            SoundIOChannelLayout layout = outstream.Layout;

            int readBytes = frame_count * outstream.BytesPerFrame;
            int readCount = 0;
            int tryCount = -1;
            int read;
            
            while (readBytes - readCount > 0 && tryCount++ < UnderflowRetryCount)
            {
                int bufferLength = (int)_ringBuffer.GetLength();
                if (bufferLength % outstream.BytesPerSample != 0)
                {
                    bufferLength -= outstream.BytesPerSample - (bufferLength % outstream.BytesPerSample);
                }
                
                read = Math.Min(bufferLength, readBytes - readCount);
                readCount += read;
                
                byte[] buffer = new byte[read];
                _ringBuffer.Dequeue(buffer);

                SoundIOChannelArea area;
                fixed (byte* buf = buffer)
                {
                    byte* ptr;
                    for (var i = 0; i < buffer.Length; i += outstream.BytesPerSample * layout.ChannelCount)
                    {
                        for (int channel = 0; layout.ChannelCount > channel; channel++)
                        {
                            area = results.GetArea(channel);
                            
                            ptr = (byte*) area.Pointer;
                            for (int j = 0; j < outstream.BytesPerSample; j++)
                            {
                                *ptr = buf[i + j + (channel * outstream.BytesPerSample)];
                                ptr++;
                            }
                            
                            area.Pointer += area.Step;
                        }
                    }
                }
                
                if (readBytes - readCount > 0)
                {
                    Underflow?.Invoke(this, new UnderflowEventArgs(readBytes - readCount));
                }
            }

            outstream.EndWrite();
        }

        public void Dispose()
        {
            _outstream?.Dispose();
            _outstream = null;
            
            Device?.RemoveReference();
            Device = null;
            
            _api?.Disconnect();
            _api?.Dispose();
            _api = null;
        }
    }
}