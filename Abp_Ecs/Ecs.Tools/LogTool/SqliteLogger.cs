using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Ecs.LogTool;

public class SqliteLogItem
{
    public int Id { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

public class SqliteLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new SqliteLogger(categoryName);
    }

    public void Dispose()
    {
        
    }
}

public class SqliteLogger : ILogger
{
    private readonly string _categoryName;

    public SqliteLogger(string categoryName)
    {
        _categoryName = categoryName;
    }

    public class EmptyScope : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new EmptyScope();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (eventId.Id != -3270 && eventId.Id != 14)
            return;

        string caller = eventId.Name;
        string msg = formatter(state, exception);

        string level = string.Empty;
        switch(logLevel)
        {
            case LogLevel.Critical:
                level = "FATAL";
                break;
            case LogLevel.Debug:
                level = "DEBUG";
                break;
            case LogLevel.Error:
                level = "ERROR";
                break;
            case LogLevel.Information:
                level = "INFO";
                break;
            case LogLevel.None:
                level = "NONE";
                break;
            case LogLevel.Trace:
                level = "TRACE";
                break;
            case LogLevel.Warning:
                level = "WARN";
                break;
        }

        SqliteLogItem logItem = new SqliteLogItem
        {
            Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Grade = level,
            Message = msg,
            Source = caller == null ? _categoryName : $"{_categoryName}.{caller}"
        };

        SqliteLogHelper.WriteLog(logItem);
    }
}

public static class SqliteLoggerExtension
{
    public static void Info(this ILogger logger, string message, [CallerMemberName] string caller = "")
    {
        logger.LogInformation(new EventId(-3270, caller), message);
    }

    public static void Error(this ILogger logger, string message, [CallerMemberName] string caller = "")
    {
        logger.LogError(new EventId(-3270, caller), message);
    }

    public static void Warn(this ILogger logger, string message, [CallerMemberName] string caller = "")
    {
        logger.LogWarning(new EventId(-3270, caller), message);
    }

    public static void Debug(this ILogger logger, string message, [CallerMemberName] string caller = "")
    {
        logger.LogDebug(new EventId(-3270, caller), message);
    }

    public static void Critical(this ILogger logger, string message, [CallerMemberName] string caller = "")
    {  
        logger.LogCritical(new EventId(-3270, caller), message);
    }
}   