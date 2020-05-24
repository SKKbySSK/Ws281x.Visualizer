using rpi_ws281x;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Ws281x.Visualizer.Helpers;
using Newtonsoft.Json;
using Ws281x.Visualizer.Models;
using Newtonsoft.Json.Serialization;
using System.IO;
using Newtonsoft.Json.Converters;

namespace Ws281x.Visualizer.Commands
{
    class LedCommand : ICommand
    {
        private Controller Controller { get; set; }

        private WS281x Device { get; set; }

        public void Execute(string[] args)
        {
            var ledCount = args.GetInt("--led");
            if (ledCount == null)
            {
                ledCount = ConsoleHelper.ReadInt("LED count > ");
            }

            var stripType = args.GetStripType();
            if (stripType == null)
            {
                stripType = ConsoleHelper.ReadWhileNull("Strip type > ", LedHelper.GetStripTypeFromName);
            }

            var pin = args.GetPin();
            if (pin == null)
            {
                pin = ConsoleHelper.ReadWhileNull("Pin > ", (read) =>
                {
                    if (int.TryParse(read, out var pin))
                    {
                        return LedHelper.GetPinFromNumber(pin);
                    }

                    return null;
                });
            }

            var controllerType = args.GetControllerType();
            if (controllerType == null)
            {
                controllerType = ConsoleHelper.ReadWhileNull("Controller type > ", LedHelper.GetControllerTypeFromName);
            }

            var settings = Settings.CreateDefaultSettings();
            var controller = settings.AddController(ledCount.Value, pin.Value, stripType.Value, controllerType.Value);
            Console.WriteLine($"LED {ledCount}, Pin {pin}, Strip {stripType}, Controller {controllerType}");

            var device = new WS281x(settings);

            Device = device;
            Controller = controller;

            Color color = Color.Black;
            RenderColor(device, controller, Color.Black);

            Shell.Run("LED", (arguments) =>
            {
                if (arguments.Length == 0)
                {
                    return;
                }

                switch (arguments[0])
                {
                    case "red":
                        color = Color.Red;
                        break;
                    case "green":
                        color = Color.Green;
                        break;
                    case "blue":
                        color = Color.Blue;
                        break;
                    case "export":
                        ExportConfig(arguments);
                        break;
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }

                RenderColor(device, controller, color);
            });

            RenderColor(device, controller, Color.Black);
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

            var led = new LedConfig()
            {
                LedCount = Controller.LEDCount,
                Pin = Controller.Pin,
                StripType = Controller.StripType,
                ControllerType = Controller.ControllerType,
            };

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var sw = new StreamWriter(path, false))
            {
                sw.Write(led.Serialize());
            }
            Console.WriteLine($"LED configuration file was exported to {path}");
        }

        private void RenderColor(WS281x device, Controller controller, Color color)
        {
            for (int i = 0; controller.LEDCount > i; i++)
            {
                controller.SetLED(i, color);
            }

            device.Render();
        }

        public void Dispose()
        {
            Device?.Dispose();
        }
    }
}
