using System;
using Spectro.Cross.Soundio;

namespace Ws281x.Visualizer.Commands
{
    public class DevicesCommand : ICommand
    {
        public SoundioInput Input { get; } = new SoundioInput();

        public SoundioOutput Output { get; } = new SoundioOutput();

        public void Execute(string[] args)
        {
            var disableInputs = false;
            var disableOutputs = false;
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "-i":
                    case "--input":
                        disableOutputs = true;
                        break;
                    case "-o":
                    case "--output":
                        disableInputs = true;
                        break;
                }
            }

            if (!disableInputs)
            {
                Console.WriteLine("Input Devices");
                Console.WriteLine("Index\tName");
                for (var i = 0; i < Input.Devices.Count; i++)
                {
                    var device = Input.Devices[i];
                    Console.WriteLine($"{i}\t{device.Name}");
                }
                Console.WriteLine();
            }

            if (!disableOutputs)
            {
                Console.WriteLine("Output Devices");
                Console.WriteLine("Index\tName");
                for (var i = 0; i < Output.Devices.Count; i++)
                {
                    var device = Output.Devices[i];
                    Console.WriteLine($"{i}\t{device.Name}");
                }
                Console.WriteLine();
            }
        }

        public void Dispose()
        {
            Input?.Dispose();
            Output?.Dispose();
        }
    }
}
