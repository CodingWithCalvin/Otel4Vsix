using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Otel4Vsix.Exceptions
{
    /// <summary>
    /// Tracks and records exceptions to OpenTelemetry.
    /// </summary>
    internal sealed class ExceptionTracker : IDisposable
    {
        private readonly ILogger _logger;
        private readonly Func<Exception, bool> _exceptionFilter;
        private readonly bool _includeVsContext;
        private bool _globalHandlerRegistered;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionTracker"/> class.
        /// </summary>
        /// <param name="logger">The logger for recording exceptions.</param>
        /// <param name="exceptionFilter">Optional filter to control which exceptions are tracked.</param>
        /// <param name="includeVsContext">Whether to include Visual Studio context in exception tracking.</param>
        public ExceptionTracker(ILogger logger, Func<Exception, bool> exceptionFilter = null, bool includeVsContext = true)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionFilter = exceptionFilter;
            _includeVsContext = includeVsContext;
        }

        /// <summary>
        /// Registers the global exception handler to capture unhandled exceptions.
        /// </summary>
        public void RegisterGlobalExceptionHandler()
        {
            ThrowIfDisposed();

            if (_globalHandlerRegistered)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            _globalHandlerRegistered = true;
        }

        /// <summary>
        /// Unregisters the global exception handler.
        /// </summary>
        public void UnregisterGlobalExceptionHandler()
        {
            if (_globalHandlerRegistered)
            {
                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                _globalHandlerRegistered = false;
            }
        }

        /// <summary>
        /// Tracks an exception and records it to telemetry.
        /// </summary>
        /// <param name="exception">The exception to track.</param>
        /// <param name="additionalAttributes">Optional additional attributes to include.</param>
        public void TrackException(Exception exception, IDictionary<string, object> additionalAttributes = null)
        {
            ThrowIfDisposed();

            if (exception == null)
            {
                return;
            }

            if (_exceptionFilter != null && !_exceptionFilter(exception))
            {
                return;
            }

            var attributes = BuildExceptionAttributes(exception, additionalAttributes);
            RecordExceptionToCurrentActivity(exception, attributes);
            LogException(exception, attributes);
        }

        /// <summary>
        /// Records an exception on the specified activity.
        /// </summary>
        /// <param name="activity">The activity to record the exception on.</param>
        /// <param name="exception">The exception to record.</param>
        /// <param name="escaped">Whether the exception escaped the scope of the span.</param>
        public void RecordExceptionOnActivity(Activity activity, Exception exception, bool escaped = true)
        {
            ThrowIfDisposed();

            if (activity == null || exception == null)
            {
                return;
            }

            if (_exceptionFilter != null && !_exceptionFilter(exception))
            {
                return;
            }

            var tags = new ActivityTagsCollection
            {
                { "exception.type", exception.GetType().FullName },
                { "exception.message", exception.Message },
                { "exception.stacktrace", exception.StackTrace ?? string.Empty },
                { "exception.escaped", escaped }
            };

            activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, tags));
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                var additionalAttributes = new Dictionary<string, object>
                {
                    { "exception.unhandled", true },
                    { "exception.terminating", e.IsTerminating }
                };

                TrackException(exception, additionalAttributes);
            }
        }

        private Dictionary<string, object> BuildExceptionAttributes(
            Exception exception,
            IDictionary<string, object> additionalAttributes)
        {
            var attributes = new Dictionary<string, object>
            {
                { "exception.type", exception.GetType().FullName },
                { "exception.message", exception.Message },
                { "exception.stacktrace", exception.StackTrace ?? string.Empty }
            };

            // Add inner exception info if present
            if (exception.InnerException != null)
            {
                attributes["exception.inner.type"] = exception.InnerException.GetType().FullName;
                attributes["exception.inner.message"] = exception.InnerException.Message;
            }

            // Add exception data
            if (exception.Data != null && exception.Data.Count > 0)
            {
                foreach (var key in exception.Data.Keys)
                {
                    var keyString = key?.ToString();
                    if (!string.IsNullOrEmpty(keyString))
                    {
                        attributes[$"exception.data.{keyString}"] = exception.Data[key]?.ToString() ?? string.Empty;
                    }
                }
            }

            // Add Visual Studio context if enabled
            if (_includeVsContext)
            {
                AddVisualStudioContext(attributes);
            }

            // Merge additional attributes
            if (additionalAttributes != null)
            {
                foreach (var kvp in additionalAttributes)
                {
                    attributes[kvp.Key] = kvp.Value;
                }
            }

            return attributes;
        }

        private void AddVisualStudioContext(Dictionary<string, object> attributes)
        {
            try
            {
                // These would be populated with actual VS context when available
                // For now, we add placeholders that can be enriched by VS-specific code
                attributes["vs.context.available"] = true;
            }
            catch
            {
                // Ignore errors when trying to get VS context
                attributes["vs.context.available"] = false;
            }
        }

        private void RecordExceptionToCurrentActivity(Exception exception, Dictionary<string, object> attributes)
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                var tags = new ActivityTagsCollection();
                foreach (var kvp in attributes)
                {
                    tags[kvp.Key] = kvp.Value;
                }

                activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, tags));
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
        }

        private void LogException(Exception exception, Dictionary<string, object> attributes)
        {
            using (_logger.BeginScope(attributes))
            {
                _logger.LogError(exception, "Exception tracked: {ExceptionType}: {ExceptionMessage}",
                    exception.GetType().Name,
                    exception.Message);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterGlobalExceptionHandler();
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ExceptionTracker));
            }
        }
    }
}
