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
        }
      }

      if (command != null)
      {
        command.Execute(args);
        command.Dispose();
      }
      else
      {
        Console.WriteLine("Ws281x.Visualizer");
      }
    }
  }
}
