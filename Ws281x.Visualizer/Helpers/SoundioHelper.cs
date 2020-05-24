using System;
using Spectro.Core;
using Spectro.Cross.Soundio;

namespace Ws281x.Visualizer.Helpers
{
    public static class SoundioHelper
    {
        public static void InitializeInputFromConsole(this SoundioInput input, AudioFormat format)
        {
            Console.WriteLine("Input devices");
            for (var i = 0; input.Devices.Count > i; i++)
            {
                Console.WriteLine($"[{i}] {input.Devices[i].Name}");
            }

            var deviceIndex = ConsoleHelper.ReadInt("Input device index > ", input.Devices.Count - 1);
            var inputDevice = input.Devices[deviceIndex];

            for (var i = 0; input.Devices.Count > i; i++)
            {
                if (i != deviceIndex)
                {
                    input.Devices[i].RemoveReference();
                }
            }

            input.Initialize(inputDevice, format);
        }

        public static void InitializeOutputFromConsole(this SoundioOutput output, AudioFormat format)
        {
            Console.WriteLine("Output devices");
            for (var i = 0; output.Devices.Count > i; i++)
            {
                Console.WriteLine($"[{i}] {output.Devices[i].Name}");
            }

            var deviceIndex = ConsoleHelper.ReadInt("Output device index > ", output.Devices.Count - 1);
            var inputDevice = output.Devices[deviceIndex];

            for (var i = 0; output.Devices.Count > i; i++)
            {
                if (i != deviceIndex)
                {
                    output.Devices[i].RemoveReference();
                }
            }

            output.Initialize(inputDevice, format);
        }

        public static bool InitializeInputDevice(this SoundioInput input, AudioFormat format, string[] args)
        {
            var index = args.GetInt("-i", "--input");
            if (index == null || index.Value >= input.Devices.Count || index.Value < 0)
            {
                return false;
            }

            var latencyMs = args.GetDouble("--input-latency");
            if (latencyMs != null)
            {
                input.DesiredLatency = TimeSpan.FromMilliseconds(latencyMs.Value);
            }

            input.Initialize(input.Devices[index.Value], format);
            return true;
        }

        public static bool InitializeOutputDevice(this SoundioOutput output, AudioFormat format, string[] args)
        {
            var index = args.GetInt("-o", "--output");
            if (index == null || index.Value >= output.Devices.Count || index.Value < 0)
            {
                return false;
            }

            var latencyMs = args.GetDouble("--output-latency");
            if (latencyMs != null)
            {
                output.DesiredLatency = TimeSpan.FromMilliseconds(latencyMs.Value);
            }

            output.Initialize(output.Devices[index.Value], format);
            return true;
        }

        public static AudioFormat InitializeFormat(AudioFormat defaultFormat = default)
        {
            int sampleRate = defaultFormat?.SampleRate ?? 44100;
            int channels = defaultFormat?.Channels ?? 2;

            sampleRate = ConsoleHelper.ReadDefaultInt($"Sample rate (Default: {sampleRate}) > ", sampleRate);
            channels = ConsoleHelper.ReadDefaultInt($"Sample rate (Default: {channels}) > ", channels);

            return new AudioFormat(sampleRate, channels);
        }
    }
}