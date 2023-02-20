using System;

namespace Inedo.Diagnostics
{
    public interface ILogSink
    {
        public void Log(IMessage message);
    }
    public interface IMessage
    {
        MessageLevel Level { get; }
        string Message { get; }
        string Category { get; }
        string Details { get; }
        object ContextData { get; }
    }

    public enum MessageLevel
    {
        Debug = 0,
        Information = 10,
        Warning = 20,
        Error = 30
    }

    public static class LogSink
    {
        public static void Log(this ILogSink sink, MessageLevel level, string message, string details, string category, object context)
        {
            sink.Log(level, message, details, category, context, null);
        }

        public static void Log(this ILogSink sink, MessageLevel level, string message, string details, string category, object context, Exception exception)
        {
            (sink ?? throw new ArgumentNullException("sink")).Log(new SimpleLogMessage(level, message, category, details, context, exception));
        }

        public static void Log(this ILogSink sink, MessageLevel level, string message, string details, string category)
        {
            sink.Log(level, message, details, category, null);
        }

        public static void Log(this ILogSink sink, MessageLevel level, string message, string details)
        {
            sink.Log(level, message, details, null, null);
        }

        public static void Log(this ILogSink sink, MessageLevel level, string message)
        {
            sink.Log(level, message, null, null, null);
        }

        public static void LogDebug(this ILogSink sink, string message, string details)
        {
            sink.Log(MessageLevel.Debug, message, details);
        }

        public static void LogDebug(this ILogSink sink, string message)
        {
            sink.LogDebug(message, null);
        }

        public static void LogInformation(this ILogSink sink, string message, string details)
        {
            sink.Log(MessageLevel.Information, message, details);
        }

        public static void LogInformation(this ILogSink sink, string message)
        {
            sink.LogInformation(message, null);
        }

        public static void LogWarning(this ILogSink sink, string message, string details)
        {
            sink.Log(MessageLevel.Warning, message, details);
        }

        public static void LogWarning(this ILogSink sink, string message)
        {
            sink.LogWarning(message, null);
        }

        public static void LogError(this ILogSink sink, string message, string details)
        {
            sink.Log(MessageLevel.Error, message, details);
        }

        public static void LogError(this ILogSink sink, string message)
        {
            sink.LogError(message, (string)null);
        }

        public static void Log(this ILogSink sink, MessageLevel level, Exception exception)
        {
            sink.Log(level, exception?.Message ?? string.Empty, exception?.ToString(), null, null, exception);
        }

        public static void LogError(this ILogSink sink, Exception exception)
        {
            sink.Log(MessageLevel.Error, exception?.Message ?? string.Empty, exception?.ToString(), null, null, exception);
        }

        public static void LogError(this ILogSink sink, string message, Exception exception)
        {
            sink.Log(MessageLevel.Error, message, exception?.ToString(), null, null, exception);
        }
    }

    [Serializable]
    public sealed class SimpleLogMessage : IMessage
    {
        public MessageLevel Level { get; }

        public string Message { get; }

        public string Category { get; }

        public string Details { get; }

        public object ContextData { get; }

        public Exception Exception { get; }

        public SimpleLogMessage(MessageLevel level, string message, string category, string details, object contextData)
            : this(level, message, category, details, contextData, null)
        {
        }

        public SimpleLogMessage(MessageLevel level, string message, string category, string details, object contextData, Exception exception)
        {
            Level = level;
            Message = message ?? string.Empty;
            Category = category;
            Details = details;
            ContextData = contextData;
            Exception = exception;
        }

        public override string ToString()
        {
            return $"{Level}: {Message}";
        }
    }

}
