using System;
using Spectro.Core;
using Ws281x.Visualizer.Commands;
using Ws281x.Visualizer.Helpers;

namespace Ws281x.Visualizer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ICommand command = null;
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "devices":
                        command = new DevicesCommand();
                        break;
                    case "passthrough":
                        command = new PassthroughCommand();
                        break;
                    case "led":
                        command = new LedCommand();
                        break;
                    case "visualize":
                        command = new VisualizeCommand();
                        break;
                }
            }

            if (command != null)
            {
                Console.CancelKeyPress += (sender, e) =>
                {
                    Console.WriteLine("Finalizing...");
                    command.Dispose();
                };

                using(command)
                {
                    command.Execute(args);
                }
            }
            else
            {
                Console.WriteLine("Ws281x.Visualizer");
            }
        }
    }
}
