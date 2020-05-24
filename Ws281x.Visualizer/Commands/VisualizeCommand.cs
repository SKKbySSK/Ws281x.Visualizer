using rpi_ws281x;
using SoundIOSharp;
using Spectro.Core;
using Spectro.Cross.Soundio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Ws281x.Visualizer.Helpers;
using Ws281x.Visualizer.Models;

namespace Ws281x.Visualizer.Commands
{
    class VisualizeCommand : ICommand
    {
        private Controller LedController { get; set; }

        private WS281x LedDevice { get; set; }

        private SoundioInput Input { get; set; }

        private SoundioOutput Output { get; set; }

        private Ws281xVisualizingOutput VisualizingOutput { get; set; }

        private Spectro.Core.Visualizer Visualizer { get; set; }

        private ThemeManager ThemeManager { get; set; }

        public void Execute(string[] args)
        {
            bool noShell = args.Contains("--no-shell");

            if (!InitLed(args))
            {
                return;
            }

            if (!InitAudio(args, noShell))
            {
                return;
            }

            var themeDir = args.GetString("--theme");
            if (!InitVisualizer(themeDir))
            {
                return;
            }

            Input.Start();
            Output?.Start();

            if (noShell)
            {
                while (true)
                {
                    Thread.Sleep(500);
                }
            }
            else
            {
                Shell.Run("Visualizer", (arguments) =>
                {
                    if (arguments.Length == 0)
                    {
                        return;
                    }

                    switch (arguments[0])
                    {
                        case "info":
                            WriteInfo();
                            break;
                        case "themes":
                            ListThemes();
                            break;
                        case "theme":
                            ThemeCommand(arguments);
                            break;
                        case "gain":
                            GainCommand(arguments);
                            break;
                        case "volume":
                            VolumeCommand(arguments);
                            break;
                        case "delay":
                            DelayCommand(arguments);
                            break;
                        case "reload-theme":
                            ReloadTheme();
                            break;
                        default:
                            Console.WriteLine("Unknown command");
                            break;
                    }
                });
            }
        }

        private void WriteInfo()
        {
            Console.WriteLine("-----Input-----");
            Console.WriteLine($"{Input.Backend}, {Input.Device.Name}, latency {Input.SoftwareLatency.TotalMilliseconds}ms");
            Console.WriteLine($"{Input.Format.SampleRate}Hz, {Input.Format.Channels}ch, {Input.Format.BitDepth}bit");

            if (Output != null)
            {
                Console.WriteLine();
                Console.WriteLine("-----Output-----");
                Console.WriteLine($"{Output.Backend}, {Output.Device.Name}, latency {Output.SoftwareLatency.TotalMilliseconds}ms");
                Console.WriteLine($"{Output.Format.SampleRate}Hz, {Output.Format.Channels}ch, {Output.Format.BitDepth}bit");
                Console.WriteLine($"Passthrough Volume: {string.Format("{0:P1}", Visualizer.Config.PassthroughVolume)}");
            }

            Console.WriteLine();
            Console.WriteLine("-----LED-----");
            Console.WriteLine($"{LedController.StripType}x{LedController.LEDCount}, {LedController.Pin}, {LedController.ControllerType}");

            var gain = VisualizingOutput.Gain;
            Console.WriteLine();
            Console.WriteLine("-----Visualizer-----");
            Console.WriteLine($"delay {VisualizingOutput.WriteDelay.TotalMilliseconds}ms, gain {(gain >= 0 ? $"+{gain}" : gain.ToString())}dB");
        }

        private void ListThemes()
        {
            foreach (var theme in ThemeManager.Themes)
            {
                Console.WriteLine($"{theme.Value.Name}\t{Path.GetFileName(theme.Key)}");
            }
        }

        private void DelayCommand(string[] arguments)
        {
            if (arguments.Length < 2)
            {
                Console.WriteLine($"Delay: {VisualizingOutput.WriteDelay.TotalMilliseconds}ms");
                return;
            }

            if (double.TryParse(arguments[1], out var delayMs))
            {
                VisualizingOutput.WriteDelay = TimeSpan.FromMilliseconds(delayMs);
                Console.WriteLine($"Delay: {VisualizingOutput.WriteDelay.TotalMilliseconds}ms");
            }
            else
            {
                Console.WriteLine($"Invalid parameter");
            }
        }

        private void VolumeCommand(string[] arguments)
        {
            if (Output == null)
            {
                Console.WriteLine("Output device is not configured. This parameter does not have any effect");
            }

            if (arguments.Length < 2)
            {
                Console.WriteLine($"Passthrough Volume: {string.Format("{0:P1}", Visualizer.Config.PassthroughVolume)}");
                return;
            }

            if (double.TryParse(arguments[1], out var volume) && volume <= 1 && volume >= 0)
            {
                Visualizer.Config.PassthroughVolume = volume;
                Console.WriteLine($"Passthrough Volume: {string.Format("{0:P1}", Visualizer.Config.PassthroughVolume)}");
            }
            else
            {
                Console.WriteLine($"Invalid parameter");
            }
        }

        private void ThemeCommand(string[] arguments)
        {
            if (arguments.Length < 2)
            {
                var current = VisualizingOutput.Theme;
                if (current == null)
                {
                    Console.WriteLine("No theme");
                    Console.WriteLine("You can see the list of themes by \'themes\' command, and set theme with \'theme <Name or File name>\'");
                }
                else
                {
                    Console.WriteLine($"Name: {current.Name}");
                }
                return;
            }

            var name = arguments[1];
            var theme = ThemeManager.Find(name);
            if (theme != null)
            {
                VisualizingOutput.Theme = theme;
                Console.WriteLine($"Name: {theme.Name}");
            }
            else
            {
                Console.WriteLine($"Theme was not found");
            }
        }

        private void GainCommand(string[] arguments)
        {
            if (arguments.Length < 2)
            {
                var current = VisualizingOutput.Gain;
                Console.WriteLine($"Gain: {(current >= 0 ? $"+{current}" : current.ToString())}dB");
                return;
            }
            
            if (double.TryParse(arguments[1], out var gain))
            {
                VisualizingOutput.Gain = gain;
                Console.WriteLine($"Gain: {(gain >= 0 ? $"+{gain}" : gain.ToString())}dB");
            }
            else
            {
                Console.WriteLine($"Invalid parameter");
            }
        }

        private void ReloadTheme()
        {
            ThemeManager.Load(onFailed: (path, ex) =>
            {
                Console.WriteLine($"-----Theme not loaded: {path}-----");
                Console.WriteLine(ex);
                Console.WriteLine("----------");
            });
        }

        #region Initializations

        private void SayInitialized(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Initialized: {msg}");
            Console.ResetColor();
        }

        private bool InitLed(string[] args)
        {
            var path = args.GetString("--led");
            LedConfig led;
            try
            {
                using var sr = new StreamReader(path);
                led = LedConfig.Deserialize(sr.ReadToEnd());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("Failed to open the audio configuration file");
                return false;
            }

            try
            {
                var settings = Settings.CreateDefaultSettings();
                var controller = settings.AddController(led.LedCount, led.Pin, led.StripType, led.ControllerType);
                var device = new WS281x(settings);

                LedDevice = device;
                LedController = controller;
            }
            catch (WS281xException ex)
            {
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Failed to open the LED device");
                Console.WriteLine("Try executing this program with root privilege");
                Console.ResetColor();
                return false;
            }

            SayInitialized($"LED {led.StripType}x{led.LedCount}, {led.Pin}, {led.ControllerType}");
            return true;
        }

        private bool InitAudio(string[] args, bool noShell)
        {
            var path = args.GetString("--audio");
            AudioConfig audio;
            try
            {
                using var sr = new StreamReader(path);
                audio = AudioConfig.Deserialize(sr.ReadToEnd());
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Failed to open the audio configuration file");
                Console.ResetColor();
                return false;
            }

            Input = new SoundioInput();
            SoundIODevice inputDevice = null;
            foreach(var device in Input.Devices)
            {
                if (device.Name == audio.InputDevice)
                {
                    inputDevice = device;
                    break;
                }
            }

            if (inputDevice != null)
            {
                Input.Initialize(inputDevice, audio.Format);
                SayInitialized($"Input {inputDevice.Name}, latency {Input.SoftwareLatency.TotalMilliseconds}ms");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Input device was not found ({audio.InputDevice})");
                Console.ResetColor();
                return false;
            }

            if (audio.OutputDevice != null)
            {
                SoundIODevice outputDevice = null;
                Output = new SoundioOutput();
                foreach (var device in Output.Devices)
                {
                    if (device.Name == audio.OutputDevice)
                    {
                        outputDevice = device;
                        break;
                    }
                }

                if (outputDevice != null)
                {
                    Output.Initialize(outputDevice, audio.Format);
                    SayInitialized($"Output {outputDevice.Name}, latency {Output.SoftwareLatency.TotalMilliseconds}ms");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Output device was not found ({audio.OutputDevice})");
                    Console.ResetColor();
                    if (noShell)
                    {
                        return false;
                    }
                    else
                    {
                        return ConsoleHelper.ReadYesNo("Proceed anyway? [yes/no] > ");
                    }
                }
            }

            return true;
        }

        private bool InitVisualizer(string themeDirectory)
        {
            var visualizingOutput = new Ws281xVisualizingOutput(LedDevice, LedController);
            visualizingOutput.WriteDelay = Output.SoftwareLatency;

            var visualizerConfig = new VisualizerConfig(Input, visualizingOutput, Output);
            var visualizer = new Spectro.Core.Visualizer(1024, Input.Format, visualizerConfig);
            visualizer.PrepareBuffer();

            visualizer.Start();
            visualizingOutput.Start();

            VisualizingOutput = visualizingOutput;
            Visualizer = visualizer;

            SayInitialized("Visualizer");

            ThemeManager = new ThemeManager(string.IsNullOrEmpty(themeDirectory) ? @"themes" : themeDirectory);
            ReloadTheme();

            var defaultTheme = ThemeManager.Find("Default");
            if (defaultTheme != null)
            {
                VisualizingOutput.Theme = defaultTheme;
            }

            SayInitialized("Theme Manager");

            return true;
        }

        #endregion

        public void Dispose()
        {
            Output?.Dispose();
            Input?.Dispose();
            LedDevice?.Dispose();
            Visualizer?.Stop(true);
            VisualizingOutput?.Stop(true);
            VisualizingOutput?.Dispose();
        }
    }
}
