using System;
using System.Collections.Generic;
using Spectro.Core;
using Spectro.Cross.Soundio;
using Ws281x.Visualizer.Helpers;
using System.Threading;
using System.Linq;
using System.IO;
using Ws281x.Visualizer.Models;

namespace Ws281x.Visualizer.Commands
{
    public class PassthroughCommand : ICommand
    {
        private SoundioInput Input { get; } = new SoundioInput();

        private SoundioOutput Output { get; set; }

        public void Execute(string[] args)
        {
            Console.WriteLine();
            var format = SoundioHelper.InitializeFormat(new AudioFormat(48000, 2));
            Console.WriteLine();

            if (!Input.InitializeInputDevice(format, args))
            {
                Input.InitializeInputFromConsole(format);
            }
            Console.WriteLine($"Input : {Input.Device.Name}, latency {Input.SoftwareLatency.TotalMilliseconds}ms");
            Console.WriteLine();

            if (!args.Contains("--no-out"))
            {
                Output = new SoundioOutput();
                if (!Output.InitializeOutputDevice(format, args))
                {
                    Output.InitializeOutputFromConsole(format);
                }
                Console.WriteLine($"Output : {Output.Device.Name}, latency {Output.SoftwareLatency.TotalMilliseconds}ms");
                Console.WriteLine();
            }

            Input.Filled += InputOnFilled;
            Input.Start();
            Output?.Start();

            Shell.Run("Passthrough", (arguments) => 
            {
                if (arguments.Length == 0)
                {
                    return;
                }

                switch (arguments[0])
                {
                    case "export":
                        ExportConfig(arguments);
                        break;
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            });
        }

        private void ExportConfig(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: export <PATH>");
                return;
            }

            var path = args[1];

            if (File.Exists(path))
            {
                var overwrite = ConsoleHelper.ReadYesNo($"{path} has already exists. Do you want to overwrite it? [yes/no] > ");
                if (!overwrite)
                {
                    return;
                }
            }

            var audio = new AudioConfig()
            {
                Format = Input.Format,
                InputDevice = Input.Device.Name,
                OutputDevice = Output.Device.Name,
            };

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var sw = new StreamWriter(path, false))
            {
                sw.Write(audio.Serialize());
            }
            Console.WriteLine($"Audio configuration file was exported to {path}");
        }

        private void InputOnFilled(object? sender, FillEventArgs e)
        {
            Output?.Write(e.Buffer, e.Offset, e.Count);
        }

        public void Dispose()
        {
            Output?.Dispose();
            Input.Dispose();
        }
    }
}
