using System;
using System.IO;
using System.Threading.Tasks;
using SoundIOSharp;
using Spectro.Core;
using Spectro.Cross.Soundio;

namespace Soundio.Test
{
    public static class InputTest
    {
        static SoundioInput input = new SoundioInput(SoundIOBackend.Alsa);
        static FileStream rawStream = new FileStream("test.raw", FileMode.Create, FileAccess.Write);
        
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

            AudioFormat format = new AudioFormat(48000, 2);
            input.SetDevice(inputDevice);
            await input.InitializeAsync(format);
            Console.WriteLine($"{input.Format.SampleRate} {input.Format.Channels}");

            await input.StartAsync();
        }

        private static void InputOnFilled(object sender, FillEventArgs e)
        {
            rawStream.Write(e.Buffer, e.Offset, e.Count);
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
            rawStream?.Dispose();
            input?.Dispose();
        }
    }
}