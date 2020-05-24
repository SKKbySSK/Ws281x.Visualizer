using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SoundIOSharp;
using Spectro.Core;

namespace Spectro.Cross.Soundio
{
    public class SoundioInput : IAudioInput, IDisposable
    {
        private readonly SoundIO _api = new SoundIO();
        private SoundIOInStream _instream;
        private RingBuffer<byte> _ringBuffer;
        private TimeSpan _bufferDuration;
        private byte[] _flushBuffer;
        
        public SoundioInput(SoundIOBackend? backend = null, TimeSpan? bufferDuration = null)
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
            
            for (int i = 0; _api.InputDeviceCount > i; i++)
            {
                var device = _api.GetInputDevice(i);
                if (i == _api.DefaultInputDeviceIndex)
                {
                    DefaultDevice = device;
                }

                Devices.Add(device);
            }
        }

        public SoundIOBackend Backend => _api.CurrentBackend;
        
        public SoundIODevice Device { get; private set; }

        public List<SoundIODevice> Devices { get; } = new List<SoundIODevice>();

        public SoundIODevice DefaultDevice { get; }
        
        public AudioFormat Format { get; private set; }
        
        public event EventHandler<FillEventArgs> Filled;

        public event EventHandler Overflow;

        public event EventHandler UnrecoverableError;

        public TimeSpan DesiredLatency { get; set; } = TimeSpan.FromMilliseconds(10);

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

        public FormatResult SupportsFormatAsync(AudioFormat format)
        {
            return checkFormatInternal(format, out _);
        }

        public void Start()
        {
            if (_instream == null)
            {
                throw new Exception("You MUST call Initialize()");
            }

            _instream.Start();
        }

        public void Stop()
        {
            if (_instream == null)
            {
                throw new Exception("You MUST call Initialize()");
            }

            _instream.Dispose();
        }

        public TimeSpan SoftwareLatency { get; private set; }

        public int FlushCount { get; set; } = 32;

        private void initInternal(AudioFormat format)
        {
            if (Device == null)
            {
                throw new Exception("No device is selected");
            }

            if (Device.ProbeError != 0)
            {
                throw new Exception($"Probe Error : {Device.ProbeError}");
            }
            
            var native = Soundio.ToSoundioFormat(format);
            if (!native.HasValue)
            {
                throw new NotSupportedException("Format is not supported : " + format);
            }

            _instream = Device.CreateInStream();
            _instream.Format = native.Value;
            _instream.SampleRate = format.SampleRate;
            _instream.ReadCallback = ReadCallback;
            _instream.OverflowCallback = () => Overflow?.Invoke(this, EventArgs.Empty);
            _instream.ErrorCallback = () => UnrecoverableError?.Invoke(this, EventArgs.Empty);
            _instream.SoftwareLatency = DesiredLatency.TotalSeconds;
            _instream.Open();
            
            // Open後にチャンネルは設定しないと動作しない模様
            if (Device.CurrentLayout.ChannelCount != format.Channels)
            {
                checkFormatInternal(format, out var channelLayout);
                if (!channelLayout.HasValue)
                {
                    throw new NotSupportedException("No suitable channel layout found : " + format.Channels);
                }
            
                _instream.Layout = channelLayout.Value;
            }
            _instream.SoftwareLatency = DesiredLatency.TotalSeconds;
            
            Format = Soundio.ToManagedFormat(_instream.Format, _instream.SampleRate, _instream.Layout.ChannelCount);
            SoftwareLatency = TimeSpan.FromSeconds(_instream.SoftwareLatency);
            

            var bytesPerSample = _instream.BytesPerSample;
            var capacity = Format.SampleRate * Format.Channels * bytesPerSample *
                           _bufferDuration.TotalSeconds;
            _ringBuffer = new RingBuffer<byte>((uint)capacity);
        }

        private FormatResult checkFormatInternal(AudioFormat format, out SoundIOChannelLayout? layout)
        {
            layout = null;
            if (!Device.SupportsSampleRate(format.SampleRate))
            {
                return FormatResult.UnsupportedSampleRate;
            }

            bool invalidChannel = true;
            foreach (var l in Device.Layouts)
            {
                if (l.ChannelCount == format.Channels)
                {
                    invalidChannel = false;
                    layout = l;
                    break;
                }
            }

            if (invalidChannel)
            {
                return FormatResult.UnsupportedChannel;
            }

            var nativeFormat = Soundio.ToSoundioFormat(format);
            if (nativeFormat == null || !Device.SupportsFormat(nativeFormat.Value))
            {
                return FormatResult.UnsupportedBitDepth;
            }

            return FormatResult.Ok;
        }
        
        private unsafe void ReadCallback(int frameCountMin, int frameCountMax)
        {
            int writeFrames = frameCountMax;
            int framesLeft = writeFrames;
            UnionBuffer unionBuffer = new UnionBuffer();

            for (; ; ) {
                int frameCount = framesLeft;

                var areas = _instream.BeginRead (ref frameCount);

                if (frameCount == 0)
                    break;

                if (areas.IsEmpty) {
                    // Due to an overflow there is a hole. Fill the ring buffer with
                    // silence for the size of the hole.
                    Console.Error.WriteLine ("Dropped {0} frames due to internal overflow", frameCount);
                } else {
                    for (int frame = 0; frame < frameCount; frame += 1) {
                        int chCount = _instream.Layout.ChannelCount;
                        int copySize = _instream.BytesPerSample;
                        unionBuffer.Bytes = new byte[copySize];
                        
                        fixed (byte* buffer = unionBuffer.Bytes)
                        {
                            for (int ch = 0; ch < chCount; ch += 1) {
                                var area = areas.GetArea (ch);
                                Buffer.MemoryCopy((void*)area.Pointer, buffer, copySize, copySize);
                                _ringBuffer.Enqueue(unionBuffer.Bytes, 0, copySize);
                                area.Pointer += area.Step;
                            }
                        }
                    }
                }

                _instream.EndRead ();

                framesLeft -= frameCount;
                if (framesLeft <= 0)
                    break;
            }

            int length = (int)_ringBuffer.GetLength();
            if (length >= FlushCount)
            {
                if (_flushBuffer == null || _flushBuffer.Length != length)
                {
                    _flushBuffer = new byte[length];
                }
                
                _ringBuffer.Dequeue(_flushBuffer);
                Filled?.Invoke(this, new FillEventArgs(_flushBuffer, 0, length));
            }
        }

        public void Dispose()
        {
            _instream?.Dispose();
            _instream = null;
            
            Device?.RemoveReference();
            Device = null;
            
            _api?.Disconnect();
            _api?.Dispose();
        }
    }
}
