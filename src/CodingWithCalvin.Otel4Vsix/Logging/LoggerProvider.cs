using System;
using Microsoft.Extensions.Logging;

namespace Otel4Vsix.Logging
{
    /// <summary>
    /// Provides and manages logging infrastructure with OpenTelemetry integration.
    /// </summary>
    internal sealed class LoggerProvider : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _defaultLogger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerProvider"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to use.</param>
        /// <param name="categoryName">The default category name for logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when loggerFactory is null.</exception>
        public LoggerProvider(ILoggerFactory loggerFactory, string categoryName)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _defaultLogger = _loggerFactory.CreateLogger(categoryName ?? "Otel4Vsix");
        }

        /// <summary>
        /// Gets the default logger instance.
        /// </summary>
        public ILogger Logger
        {
            get
            {
                ThrowIfDisposed();
                return _defaultLogger;
            }
        }

        /// <summary>
        /// Gets the logger factory for creating additional loggers.
        /// </summary>
        public ILoggerFactory LoggerFactory
        {
            get
            {
                ThrowIfDisposed();
                return _loggerFactory;
            }
        }

        /// <summary>
        /// Creates a logger with the specified category name.
        /// </summary>
        /// <param name="categoryName">The category name for the logger.</param>
        /// <returns>A new logger instance.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            ThrowIfDisposed();
            return _loggerFactory.CreateLogger(categoryName);
        }

        /// <summary>
        /// Creates a logger for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to use as the category name.</typeparam>
        /// <returns>A new logger instance.</returns>
        public ILogger<T> CreateLogger<T>()
        {
            ThrowIfDisposed();
            return _loggerFactory.CreateLogger<T>();
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">The message format arguments.</param>
        public void LogInformation(string message, params object[] args)
        {
            ThrowIfDisposed();
            _defaultLogger.LogInformation(message, args);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">The message format arguments.</param>
        public void LogWarning(string message, params object[] args)
        {
            ThrowIfDisposed();
            _defaultLogger.LogWarning(message, args);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">The message format arguments.</param>
        public void LogError(string message, params object[] args)
        {
            ThrowIfDisposed();
            _defaultLogger.LogError(message, args);
        }

        /// <summary>
        /// Logs an error message with an exception.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">The message format arguments.</param>
        public void LogError(Exception exception, string message, params object[] args)
        {
            ThrowIfDisposed();
            _defaultLogger.LogError(exception, message, args);
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">The message format arguments.</param>
        public void LogDebug(string message, params object[] args)
        {
            ThrowIfDisposed();
            _defaultLogger.LogDebug(message, args);
        }

        /// <summary>
        /// Logs a critical error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">The message format arguments.</param>
        public void LogCritical(string message, params object[] args)
        {
            ThrowIfDisposed();
            _defaultLogger.LogCritical(message, args);
        }

        /// <summary>
        /// Logs a critical error message with an exception.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">The message format arguments.</param>
        public void LogCritical(Exception exception, string message, params object[] args)
        {
            ThrowIfDisposed();
            _defaultLogger.LogCritical(exception, message, args);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _loggerFactory?.Dispose();
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LoggerProvider));
            }
        }
    }
}
