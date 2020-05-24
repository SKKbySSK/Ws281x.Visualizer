using System.Linq;
using Spectro.Cross.Soundio;

namespace Ws281x.Visualizer.Helpers
{
    public static class ArgumentsHelper
    {
        public static int? GetInt(this string[] args, params string[] parameters)
        {
            if (int.TryParse(args.GetString(parameters), out var value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        public static double? GetDouble(this string[] args, params string[] parameters)
        {
            if (double.TryParse(args.GetString(parameters), out var value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        public static bool? GetBool(this string[] args, params string[] parameters)
        {
            if (bool.TryParse(args.GetString(parameters), out var value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        public static string GetString(this string[] args, params string[] parameters)
        {
            foreach (var parameter in parameters)
            {
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (parameter == args[i])
                    {
                        return args[i + 1];
                    }
                }
            }

            return null;
        }
    }
}