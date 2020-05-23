using System;
using System.Collections.Generic;
using Spectro.Core;
using Spectro.Cross.Soundio;
using Ws281x.Visualizer.Helpers;
using System.Threading;

namespace Ws281x.Visualizer.Commands
{
    public class PassthroughCommand : ICommand
    {
        public SoundioInput Input { get; } = new SoundioInput();

        public SoundioOutput Output { get; } = new SoundioOutput();

        public void Execute(string[] args)
        {
            var format = new AudioFormat();

            if (!Input.InitializeInputDevice(format, args))
            {
                Console.WriteLine("Failed to initialize input");
                return;
            }

            if (!Output.InitializeOutputDevice(format, args))
            {
                Console.WriteLine("Failed to initialize output");
                return;
            }

            Input.Filled += InputOnFilled;
            Input.Start();
            Output.Start();

            if (Console.In != null)
            {
                Console.WriteLine("Press any key to exit");
                Console.Read();
            }
            else
            {
                Console.WriteLine("Playing for 10 seconds");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        private void InputOnFilled(object? sender, FillEventArgs e)
        {
            Output.Write(e.Buffer, e.Offset, e.Count);
        }

        public void Dispose()
        {
            Input?.Dispose();
            Output?.Dispose();
        }
    }
}
