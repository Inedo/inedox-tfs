using System;
using Inedo.Diagnostics;

namespace Inedo.TFS.TfsTiny
{
    internal class ConsoleLogSink : ILogSink
    {
        private ConsoleLogSink()
        {

        }

        public static ConsoleLogSink Instance => new ConsoleLogSink();

        public void Log(IMessage message)
        {
            if (message.Level == MessageLevel.Error)
            {
                Console.Error.WriteLine($"{getLogLevel(message.Level)}{message.Message}");
                if (!string.IsNullOrWhiteSpace(message.Details))
                    Console.Error.WriteLine($"{getLogLevel(message.Level)}{message.Details}");
            }
            else
            {
                Console.WriteLine($"{getLogLevel(message.Level)}{message.Message}");
                if (!string.IsNullOrWhiteSpace(message.Details))
                    Console.WriteLine($"{getLogLevel(message.Level)}{message.Details}");
            }

            string getLogLevel(MessageLevel level)
            {
                return level switch
                {
                    MessageLevel.Debug => "DEBUG: ",
                    MessageLevel.Information => "INFO: ",
                    MessageLevel.Warning => "WARN: ",
                    MessageLevel.Error => "ERROR: ",
                    _ => string.Empty
                };
            }
        }
    }
}
