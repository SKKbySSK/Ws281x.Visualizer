using System;
using System.Collections.Generic;
using System.Text;

namespace Ws281x.Visualizer.Helpers
{
    static class Shell
    {
        public static void Run(string name, Action<string[]> action, string msg = "[$TITLE] $ ")
        {
            var replacedMsg = msg.Replace("$TITLE", name);
            Console.Write(replacedMsg);
            var read = Console.ReadLine();

            while (read != "exit")
            {
                action(string.IsNullOrEmpty(read) ? new string[0] : SplitArguments(read));
                Console.Write(replacedMsg);
                read = Console.ReadLine();
            }
        }

        private static string[] SplitArguments(string commandLine)
        {
            var parmChars = commandLine.ToCharArray();
            var inSingleQuote = false;
            var inDoubleQuote = false;
            for (var index = 0; index < parmChars.Length; index++)
            {
                if (parmChars[index] == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    parmChars[index] = '\n';
                }
                if (parmChars[index] == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                    parmChars[index] = '\n';
                }
                if (!inSingleQuote && !inDoubleQuote && parmChars[index] == ' ')
                    parmChars[index] = '\n';
            }
            return (new string(parmChars)).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
