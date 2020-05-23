using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SoundIOSharp;
using Spectro.Core;
using Spectro.Cross.Soundio;

namespace Soundio.Test
{
    public static class OutputTest
    {
        private static int samplesPerPush = 1024;
        private static FileStream waveStream = new FileStream("test.wav", FileMode.Open, FileAccess.Read);
        private static ConcurrentQueue<byte[]> buffers = new ConcurrentQueue<byte[]>();
        private static SoundioOutput _output;

        public static async Task StartAsync()
        {
            waveStream.Seek(44, SeekOrigin.Begin); // Header
            _output = await InitializeOutput(new AudioFormat(48000, 2, 16));
            RunBufferLoop(_output.Format);
            
            _output.Start();
        }

        public static void Dispose()
        {
            _output.Dispose();
            waveStream.Dispose();
        }
        
        private static void RunBufferLoop(AudioFormat format)
        {
            TimeSpan sleep = TimeSpan.FromSeconds(samplesPerPush / (double)(format.SampleRate * format.Channels));
            Console.WriteLine("Sleep Time : " + sleep.TotalMilliseconds);
            Task.Run(() =>
            {
                while (waveStream.Position < waveStream.Length)
                {
                    int size = Math.Min((int)(waveStream.Length - waveStream.Position), samplesPerPush * 3);
                    var buffer = new byte[size];
                    int read = waveStream.Read(buffer, 0, buffer.Length);
                    _output.Write(buffer, 0, read);
//                    Thread.Sleep(sleep);
                }
                
                _output.Underflow -= OutputOnUnderflow;
                _output.UnderflowTimedOut -= OutputOnUnderflowTimedOut;
                _output.UnderflowTimedOut += (sender, args) =>
                {
                    Console.WriteLine("Last buffer filled");
                    _output.Stop();
                    Console.WriteLine("Output stopped");
                    _output.Dispose();
                };
            });
        }

        private static async Task<SoundioOutput> InitializeOutput(AudioFormat format)
        {
            var output = new SoundioOutput(SoundIOBackend.Alsa,  TimeSpan.FromSeconds(360));
            output.Underflow += OutputOnUnderflow;
            output.UnderflowTimedOut += OutputOnUnderflowTimedOut;
            
            for (int i = 0; i < output.Devices.Count; i++)
            {
                Console.WriteLine($"[{i}] {output.Devices[i].Name}");
            }
            var deviceIndex = ReadInt("Output device index : ");
            output.SetDevice(output.Devices[deviceIndex], format);
            output.Initialize();
            
            Console.WriteLine($"{output.Format.SampleRate} {output.Format.Channels}");

            return output;
        }

        private static void OutputOnUnderflowTimedOut(object sender, EventArgs e)
        {
            Console.WriteLine("Underflow timed out");
        }

        private static void OutputOnUnderflow(object sender, UnderflowEventArgs e)
        {
            Console.Write("Underflow!");
            if (e.Size.HasValue)
            {
                Console.WriteLine($" : {e.Size.Value} bytes required");
            }
            else
            {
                Console.WriteLine();
            }
            
            int size = Math.Min((int)(waveStream.Length - waveStream.Position), e.Size ?? 2048);
            var buffer = new byte[size];
            waveStream.Read(buffer, 0, size);
            _output.Write(buffer, 0, size);
        }
        
        private static int ReadInt(string message, string errorMessage = "Try Again [yes/no] : ")
        {
            int num;
            Console.Write(message);
            while (!int.TryParse(Console.ReadLine(), out num))
            {
                Console.Write(errorMessage);
            }

            return num;
        }
    }
}