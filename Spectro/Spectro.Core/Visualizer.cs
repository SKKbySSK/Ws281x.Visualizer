using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Spectro.Core
{
    public class VisualizerConfig
    {
        public VisualizerConfig(IAudioInput input, IVisualizingOutput output, IAudioOutput passthroughOutput = null)
        {
            Input = input;
            Output = output;
            PassthroughOutput = passthroughOutput;
        }

        public IAudioInput Input { get; }
        
        public IVisualizingOutput Output { get; }
        
        public IAudioOutput PassthroughOutput { get; set; }

        public double PassthroughVolume { get; set; } = 1;
    }
    
    public class Visualizer
    {
        private readonly int _fftBuferSize;
        private readonly object _lockObj = new object();
        
        private readonly Analyzer _analyzer;
        private RingBuffer<byte> _visualizingBuffer;
        private Thread _visualizerThread;

        public VisualizerConfig Config { get; }
        
        public AudioFormat Format { get; }

        public bool IsStarted { get; private set; } = false;
        
        public ILog Log { get; set; }
        
        public Visualizer(int fftLength, AudioFormat format, VisualizerConfig config)
        {
            _fftBuferSize = fftLength * (format.BitDepth / 8) * format.Channels;
            _analyzer = new Analyzer(fftLength, format.SampleRate);
            
            Config = config;
            Format = format;
            
            config.Input.Filled += InputOnFilled;
            config.Input.Overflow += InputOnOverflow;
            
            if (config.PassthroughOutput != null)
            {
                config.PassthroughOutput.Underflow += PassthroughOutputOnUnderflow;
                config.PassthroughOutput.UnderflowTimedOut += PassthroughOutputOnUnderflowTimedOut;
            }
        }

        public void PrepareBuffer(TimeSpan? bufferDuration = null)
        {
            var dur = bufferDuration ?? TimeSpan.FromSeconds(1);
            var bytesPerSample = Format.BitDepth / 8;
            var capacity = Format.SampleRate * Format.Channels * bytesPerSample * dur.TotalSeconds;
            _visualizingBuffer = new RingBuffer<byte>((uint)capacity);
        }

        public void Start()
        {
            Stop(true);
            IsStarted = true;
            _visualizerThread = new Thread(VisualizeThread);
            _visualizerThread.Start();
        }

        public void Stop(bool waitForExit)
        {
            IsStarted = false;
            if (waitForExit)
            {
                lock (_lockObj)
                {
                }
            }
        }

        private void VisualizeThread()
        {
            Log?.Debug("[Visualizer] Started");
            lock (_lockObj)
            {
                var fftBuffer = new byte[_fftBuferSize];
                while (IsStarted)
                {
                    try
                    {
                        var length = _visualizingBuffer.GetLength();
                        if (length >= _fftBuferSize)
                        {
                            _visualizingBuffer.Dequeue(fftBuffer);
                            _visualizingBuffer.Discard((int)(length - _fftBuferSize));
                            Analyze(fftBuffer);
                        }
                        else
                        {
                            Thread.Yield();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log?.Error("[Visualizer] Unknown exception occured\n" + ex.Message + "\n" + ex.StackTrace);
                        IsStarted = false;
                    }
                }
            }
            Log?.Debug("[Visualizer] Exited");
        }

        private void InputOnFilled(object sender, FillEventArgs e)
        {
            if (e.Count == 0)
            {
                return;
            }

            if (e.Count > _visualizingBuffer.GetCapacity())
            {
                Log?.Error($"Too many buffer received.({e.Count} bytes) Fft calculation may have incorrect result");
            }
            else
            {
                _visualizingBuffer.Enqueue(e.Buffer, e.Offset, e.Count);
            }
            
            var volume = (float) Config.PassthroughVolume;
            if (volume < 1)
            {
                volume = Math.Min(Math.Max(0, volume), 1);
                short sample;
                var volBuffer = new byte[2];
                // TODO: Fix increment size
                for (var i = 0; i < e.Count; i += 2)
                {
                    sample = (short)(BitConverter.ToInt16(e.Buffer, i + e.Offset) * volume);
                    ToBytes(sample, volBuffer, 0);
                    e.Buffer[i + e.Offset] = volBuffer[0];
                    e.Buffer[i + e.Offset + 1] = volBuffer[1];
                }
            }
            
            Config.PassthroughOutput?.Write(e.Buffer, e.Offset, e.Count);
        }
        
        static unsafe void ToBytes(short value, byte[] array, int offset)
        {
            fixed (byte* ptr = &array[offset])
                *(short*)ptr = value;
        }

        private void InputOnOverflow(object sender, EventArgs e)
        {
            Log?.Warning("[Input] Overflow");
        }

        private void PassthroughOutputOnUnderflowTimedOut(object sender, EventArgs e)
        {
            Log?.Warning("[Passthrough Output] Timed out");
        }

        private void PassthroughOutputOnUnderflow(object sender, UnderflowEventArgs e)
        {
            if (e.Size.HasValue)
            {
                Log?.Warning($"[Passthrough Output] Underflow {e.Size.Value} bytes");
            }
            else
            {
                Log?.Warning($"[Passthrough Output] Underflow");
            }
        }

        private void Analyze(byte[] buffer)
        {
            try
            {   int bytesPerSample = Format.BitDepth / 8;
                var dBuffer = new double[buffer.Length / bytesPerSample];
                int length = buffer.Length;
                if (length % 2 != 0)
                {
                    length--;
                }

                for (int i = 0; i < length; i += 2)
                {
                    dBuffer[i / 2] = BitConverter.ToInt16(buffer, i) / (double)short.MaxValue;
                }
            
                var monoBuffer = new double[dBuffer.Length / Format.Channels];
                for (int i = 0; i < dBuffer.Length; i += Format.Channels)
                {
                    for (int channel = 0; channel < Format.Channels; channel++)
                    {
                        monoBuffer[i / Format.Channels] += dBuffer[i + channel];
                    }

                    monoBuffer[i / Format.Channels] /= Format.Channels;
                }

                var res = _analyzer.Fft(monoBuffer, 0);
                Config.Output.Update(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while analyzing buffer");
                Console.WriteLine(ex);
            }
        }
    }
}
