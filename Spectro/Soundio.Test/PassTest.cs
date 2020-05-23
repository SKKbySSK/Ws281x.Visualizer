using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SoundIOSharp;
using Spectro.Core;
using Spectro.Cross.Soundio;

namespace Soundio.Test
{
    public static class PassTest
    {
        static SoundioInput input = new SoundioInput(SoundIOBackend.Alsa);
        private static int samplesPerPush = 1024 * 10;
        private static BufferSink<byte> _sink = new BufferSink<byte>();
        private static SoundioOutput _output;
        
        public static async Task StartAsync()
        {
            input.Filled += InputOnFilled;
            Console.WriteLine("Input devices");
            for (var i = 0; input.Devices.Count > i; i++)
            {
                Console.WriteLine($"[{i}] {input.Devices[i].Name}");
            }

            var deviceIndex = ReadInt("Input device index : ");
            var inputDevice = input.Devices[deviceIndex];

            for (var i = 0; input.Devices.Count > i; i++)
            {
                if (i != deviceIndex)
                {
                    input.Devices[i].RemoveReference();
                }
            }

            AudioFormat format = new AudioFormat(48000, 2, 16);
            input.SetDevice(inputDevice);
            await input.InitializeAsync(format);
            Console.WriteLine($"{input.Format.SampleRate} {input.Format.Channels}");

            _output = await InitializeOutput(format);
            await input.StartAsync();
//            Console.WriteLine("Buffering started");
//            Console.WriteLine("Press any key to start playback...");
//            Console.ReadKey();
            _output.Start();
        }

        private static void InputOnFilled(object sender, FillEventArgs e)
        {
            _output?.Write(e.Buffer, e.Offset, e.Count);
        }

        private static async Task<SoundioOutput> InitializeOutput(AudioFormat format)
        {
            var output = new SoundioOutput(SoundIOBackend.Alsa);
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

        public static void Dispose()
        {
            _output?.Dispose();
            input?.Dispose();
        }
    }
}
