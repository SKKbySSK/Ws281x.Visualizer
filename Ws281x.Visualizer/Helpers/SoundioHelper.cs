using System;
using Spectro.Core;
using Spectro.Cross.Soundio;

namespace Ws281x.Visualizer.Helpers
{
    public static class SoundioHelper
    {
        public static SoundioInput InitializeInputFromConsole(AudioFormat format)
        {
            var input = new SoundioInput();
            Console.WriteLine("Input devices");
            for (var i = 0; input.Devices.Count > i; i++)
            {
                Console.WriteLine($"[{i}] {input.Devices[i].Name}");
            }

            var deviceIndex = ReadInt("Input device index > ", input.Devices.Count - 1);
            var inputDevice = input.Devices[deviceIndex];

            for (var i = 0; input.Devices.Count > i; i++)
            {
                if (i != deviceIndex)
                {
                    input.Devices[i].RemoveReference();
                }
            }

            input.Initialize(inputDevice, format);
            return input;
        }

        public static SoundioOutput InitializeOutputFromConsole(AudioFormat format)
        {
            var output = new SoundioOutput();
            Console.WriteLine("Output devices");
            for (var i = 0; output.Devices.Count > i; i++)
            {
                Console.WriteLine($"[{i}] {output.Devices[i].Name}");
            }

            var deviceIndex = ReadInt("Output device index > ", output.Devices.Count - 1);
            var inputDevice = output.Devices[deviceIndex];

            for (var i = 0; output.Devices.Count > i; i++)
            {
                if (i != deviceIndex)
                {
                    output.Devices[i].RemoveReference();
                }
            }

            output.Initialize(inputDevice, format);
            return output;
        }

        public static bool InitializeInputDevice(this SoundioInput input, AudioFormat format, string[] args)
        {
            var index = args.GetInt("-i", "--input");
            if (index == null || index.Value >= input.Devices.Count || index.Value < 0)
            {
                Console.WriteLine("Invalid input device index");
                return false;
            }

            input.Initialize(input.Devices[index.Value], format);
            return true;
        }

        public static bool InitializeOutputDevice(this SoundioOutput output, AudioFormat format, string[] args)
        {
            var index = args.GetInt("-o", "--output");
            if (index == null || index.Value >= output.Devices.Count || index.Value < 0)
            {
                Console.WriteLine("Invalid output device index");
                return false;
            }

            output.Initialize(output.Devices[index.Value], format);
            return true;
        }

        public static AudioFormat InitializeFormat(AudioFormat defaultFormat = default)
        {
            int sampleRate = defaultFormat?.SampleRate ?? 44100;
            int channels = defaultFormat?.Channels ?? 2;

            sampleRate = ReadDefaultInt($"Sample rate (Default: {sampleRate}) > ", sampleRate);
            channels = ReadDefaultInt($"Sample rate (Default: {channels}) > ", channels);

            return new AudioFormat(sampleRate, channels);
        }

        public static int ReadInt(string message, int max = int.MaxValue, int min = int.MinValue, string errorMessage = "Try Again > ")
        {
            int num;
            Console.Write(message);
            while (!int.TryParse(Console.ReadLine(), out num) || num < min || num > max)
            {
                Console.Write(errorMessage);
            }

            return num;
        }

        public static int ReadDefaultInt(string message, int defaultValue, int max = int.MaxValue, int min = int.MinValue, string errorMessage = "Try Again > ")
        {
            int num;
            Console.Write(message);
            var read = Console.ReadLine();
            while (!int.TryParse(read, out num) || num < min || num > max)
            {
                if (string.IsNullOrEmpty(read))
                {
                    return defaultValue;
                }

                Console.Write(errorMessage);
                read = Console.ReadLine();
            }

            return num;
        }

        public static bool ReadYesNo(string message, string errorMessage = "Try Again [yes/no] > ")
        {
            Console.Write(message);
            string res = Console.ReadLine()?.ToLower() ?? "";
            while (res != "yes" && res != "no")
            {
                Console.Write(errorMessage);
                res = Console.ReadLine()?.ToLower() ?? "";
            }

            return res == "yes";
        }
    }
}