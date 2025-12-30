using System;
using Microsoft.Extensions.Logging;

namespace CodingWithCalvin.Otel4Vsix.Exporters;

/// <summary>
/// A logger provider that writes to <see cref="System.Diagnostics.Debug"/>.
/// </summary>
internal sealed class DebugLoggerProvider : ILoggerProvider
{
    private readonly string _serviceName;
    private readonly string _serviceVersion;

    public DebugLoggerProvider(string serviceName, string serviceVersion)
    {
        _serviceName = serviceName;
        _serviceVersion = serviceVersion;
    }

    public ILogger CreateLogger(string categoryName) => new DebugLogger(_serviceName, _serviceVersion, categoryName);

    public void Dispose() { }
}

/// <summary>
/// A logger that writes to <see cref="System.Diagnostics.Debug"/>.
/// </summary>
internal sealed class DebugLogger : ILogger
{
    private readonly string _prefix;
    private readonly string _categoryName;

    public DebugLogger(string serviceName, string serviceVersion, string categoryName)
    {
        _prefix = $"[{serviceName} v{serviceVersion}]";
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        var levelStr = logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT",
            _ => "NONE"
        };

        System.Diagnostics.Trace.WriteLine($"{_prefix} [{levelStr}] {_categoryName}: {message}");

        if (exception != null)
        {
            System.Diagnostics.Trace.WriteLine($"  Exception: {exception}");
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new NullScope();
        public void Dispose() { }
    }
}
