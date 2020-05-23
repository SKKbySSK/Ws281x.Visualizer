using System;

namespace Spectro.Core
{
    public enum LogLevel
    {
        None = 0,
        Verbose = 1,
        Debug = 2,
        Warning = 3,
        Error = 4,
    }
    
    public interface ILog
    {
        LogLevel Level { get; set; }
        
        void Verbose(object message);
        
        void Debug(object message);
        
        void Warning(object message);
        
        void Error(object message);
    }

    public class ConsoleLog : ILog
    {
        public LogLevel Level { get; set; } = LogLevel.Debug;

        public void Verbose(object message)
        {
            WriteLog(message, LogLevel.Verbose);
        }

        public void Debug(object message)
        {
            WriteLog(message, LogLevel.Debug);
        }

        public void Warning(object message)
        {
            WriteLog(message, LogLevel.Warning);
        }

        public void Error(object message)
        {
            WriteLog(message, LogLevel.Error);
        }

        private void WriteLog(object message, LogLevel level)
        {
            if (level < Level)
            {
                return;
            }
            
            Console.WriteLine(message);
        }
    }
}
