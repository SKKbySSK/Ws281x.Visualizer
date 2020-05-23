using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Spectro.Core;
using Spectro.Cross.Soundio;

namespace Soundio.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await UnderflowPassTest.StartAsync();
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            UnderflowPassTest.Dispose();
        }

        private static async Task<SoundioInput> InitializeInput()
        {
            var input = new SoundioInput();
            
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
            await input.InitializeAsync(format);
            Console.WriteLine("Device initialized");

            return input;
        }
        
        public static int ReadInt(string message, string errorMessage = "Try Again [yes/no] : ")
        {
            int num;
            Console.Write(message);
            while (!int.TryParse(Console.ReadLine(), out num))
            {
                Console.Write(errorMessage);
            }

            return num;
        }

        public static bool ReadYesNo(string message, string errorMessage = "Try Again : ")
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
