using System;
using System.Collections.Generic;
using System.Text;

namespace Ws281x.Visualizer.Helpers
{
    public static class ConsoleHelper
    {
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

        public static string ReadWhile(string message, Func<string, bool> predicate, string errorMessage = "Try Again [yes/no] > ")
        {
            Console.Write(message);
            var read = Console.ReadLine();
            while (!predicate(read))
            {
                Console.Write(errorMessage);
                read = Console.ReadLine();
            }

            return read;
        }

        public static T ReadWhileNull<T>(string message, Func<string, T> convert, string errorMessage = "Try Again > ") where T : class
        {
            Console.Write(message);
            var read = Console.ReadLine();
            var result = convert(read);
            while (result == null)
            {
                Console.Write(errorMessage);
                read = Console.ReadLine();
                result = convert(read);
            }

            return result;
        }

        public static T ReadWhileNull<T>(string message, Func<string, T?> convert, string errorMessage = "Try Again > ") where T : struct
        {
            Console.Write(message);
            var read = Console.ReadLine();
            var result = convert(read);
            while (result == null)
            {
                Console.Write(errorMessage);
                read = Console.ReadLine();
                result = convert(read);
            }

            return result.Value;
        }
    }
}
