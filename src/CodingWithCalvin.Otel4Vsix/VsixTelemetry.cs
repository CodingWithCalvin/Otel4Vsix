using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using CodingWithCalvin.Otel4Vsix.Exceptions;
using CodingWithCalvin.Otel4Vsix.Logging;
using CodingWithCalvin.Otel4Vsix.Tracing;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CodingWithCalvin.Otel4Vsix;

/// <summary>
/// Main entry point for OpenTelemetry in Visual Studio extensions.
/// Provides static access to tracing, metrics, logging, and exception tracking.
/// </summary>
/// <remarks>
/// This class is thread-safe. Call <see cref="Initialize"/> once during extension initialization,
/// then access telemetry through the static properties. Call <see cref="Shutdown"/> during disposal.
/// </remarks>
public static class VsixTelemetry
{
    private static readonly object _lock = new object();
    private static volatile bool _isInitialized;
    private static TelemetryConfiguration _configuration;
    private static ActivitySourceProvider _activitySourceProvider;
    private static Metrics.MetricsProvider _metricsProvider;
    private static Logging.LoggerProvider _loggerProvider;
    private static ExceptionTracker _exceptionTracker;
    private static TracerProvider _tracerProvider;
    private static OpenTelemetry.Metrics.MeterProvider _otelMeterProvider;
    private static ILoggerFactory _loggerFactory;

    /// <summary>
    /// Gets a value indicating whether telemetry has been initialized.
    /// </summary>
    public static bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets the <see cref="ActivitySource"/> for creating traces.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when telemetry is not initialized.</exception>
    public static ActivitySource Tracer
    {
        get
        {
            ThrowIfNotInitialized();
            return _activitySourceProvider.ActivitySource;
        }
    }

    /// <summary>
    /// Gets the <see cref="System.Diagnostics.Metrics.Meter"/> for creating metrics.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when telemetry is not initialized.</exception>
    public static Meter Meter
    {
        get
        {
            ThrowIfNotInitialized();
            return _metricsProvider.Meter;
        }
    }

    /// <summary>
    /// Gets the default logger for logging. Internal use only - use LogInformation, LogWarning, etc. methods instead.
    /// </summary>
    internal static ILogger Logger
    {
        get
        {
            ThrowIfNotInitialized();
            return _loggerProvider.Logger;
        }
    }

    /// <summary>
    /// Gets the logger factory for creating additional loggers. Internal use only.
    /// </summary>
    internal static ILoggerFactory LoggerFactory
    {
        get
        {
            ThrowIfNotInitialized();
            return _loggerProvider.LoggerFactory;
        }
    }

    /// <summary>
    /// Initializes telemetry with the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration options for telemetry.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when telemetry is already initialized.</exception>
    public static void Initialize(TelemetryConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        configuration.Validate();

        lock (_lock)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException(
                    "VsixTelemetry is already initialized. Call Shutdown() before reinitializing.");
            }

            _configuration = configuration;
            InitializeProviders();
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Shuts down telemetry and releases all resources.
    /// </summary>
    /// <param name="timeoutMilliseconds">Optional timeout for flushing pending telemetry.</param>
    public static void Shutdown(int timeoutMilliseconds = 5000)
    {
        lock (_lock)
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                // Dispose in reverse order of initialization
                _exceptionTracker?.Dispose();
                _loggerProvider?.Dispose();
                _metricsProvider?.Dispose();
                _activitySourceProvider?.Dispose();

                // Shutdown OpenTelemetry providers
                _otelMeterProvider?.Shutdown(timeoutMilliseconds);
                _tracerProvider?.Shutdown(timeoutMilliseconds);

                _otelMeterProvider?.Dispose();
                _tracerProvider?.Dispose();
                _loggerFactory?.Dispose();
            }
            finally
            {
                _exceptionTracker = null;
                _loggerProvider = null;
                _metricsProvider = null;
                _activitySourceProvider = null;
                _otelMeterProvider = null;
                _tracerProvider = null;
                _loggerFactory = null;
                _configuration = null;
                _isInitialized = false;
            }
        }
    }

    /// <summary>
    /// Tracks an exception and records it to telemetry.
    /// </summary>
    /// <param name="exception">The exception to track.</param>
    /// <param name="additionalAttributes">Optional additional attributes to include.</param>
    public static void TrackException(Exception exception, IDictionary<string, object> additionalAttributes = null)
    {
        if (!_isInitialized || _exceptionTracker == null)
        {
            return;
        }

        _exceptionTracker.TrackException(exception, additionalAttributes);
    }

    /// <summary>
    /// Starts a new activity with the specified name.
    /// </summary>
    /// <param name="name">The name of the activity.</param>
    /// <param name="kind">The kind of activity.</param>
    /// <returns>The started activity, or null if telemetry is not initialized or no listeners are registered.</returns>
    public static Activity StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        if (!_isInitialized || _activitySourceProvider == null)
        {
            return null;
        }

        return _activitySourceProvider.StartActivity(name, kind);
    }

    /// <summary>
    /// Starts a new activity for a VS command execution.
    /// </summary>
    /// <param name="commandName">The name of the command being executed.</param>
    /// <returns>The started activity, or null if telemetry is not initialized or no listeners are registered.</returns>
    public static Activity StartCommandActivity(string commandName)
    {
        if (!_isInitialized || _activitySourceProvider == null)
        {
            return null;
        }

        return _activitySourceProvider.StartCommandActivity(commandName);
    }

    /// <summary>
    /// Creates a logger for the specified type. Internal use only.
    /// </summary>
    /// <typeparam name="T">The type to use as the category name.</typeparam>
    /// <returns>A new logger instance, or null if telemetry is not initialized.</returns>
    internal static ILogger<T> CreateLogger<T>()
    {
        if (!_isInitialized || _loggerProvider == null)
        {
            return null;
        }

        return _loggerProvider.CreateLogger<T>();
    }

    /// <summary>
    /// Creates a logger with the specified category name.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>A new logger instance, or null if telemetry is not initialized.</returns>
    internal static ILogger CreateLogger(string categoryName)
    {
        if (!_isInitialized || _loggerProvider == null)
        {
            return null;
        }

        return _loggerProvider.CreateLogger(categoryName);
    }

    #region Logging Wrapper Methods

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogInformation(string message, params object[] args)
    {
        if (_isInitialized && _loggerProvider?.Logger != null)
        {
            _loggerProvider.Logger.LogInformation(message, args);
        }
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogWarning(string message, params object[] args)
    {
        if (_isInitialized && _loggerProvider?.Logger != null)
        {
            _loggerProvider.Logger.LogWarning(message, args);
        }
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogError(string message, params object[] args)
    {
        if (_isInitialized && _loggerProvider?.Logger != null)
        {
            _loggerProvider.Logger.LogError(message, args);
        }
    }

    /// <summary>
    /// Logs an error message with an exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message template.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogError(Exception exception, string message, params object[] args)
    {
        if (_isInitialized && _loggerProvider?.Logger != null)
        {
            _loggerProvider.Logger.LogError(exception, message, args);
        }
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogDebug(string message, params object[] args)
    {
        if (_isInitialized && _loggerProvider?.Logger != null)
        {
            _loggerProvider.Logger.LogDebug(message, args);
        }
    }

    /// <summary>
    /// Logs a trace message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogTrace(string message, params object[] args)
    {
        if (_isInitialized && _loggerProvider?.Logger != null)
        {
            _loggerProvider.Logger.LogTrace(message, args);
        }
    }

    /// <summary>
    /// Logs a critical message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogCritical(string message, params object[] args)
    {
        if (_isInitialized && _loggerProvider?.Logger != null)
        {
            _loggerProvider.Logger.LogCritical(message, args);
        }
    }

    /// <summary>
    /// Logs a critical message with an exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message template.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogCritical(Exception exception, string message, params object[] args)
    {
        if (_isInitialized && _loggerProvider?.Logger != null)
        {
            _loggerProvider.Logger.LogCritical(exception, message, args);
        }
    }

    #endregion

    /// <summary>
    /// Gets or creates a counter with the specified name.
    /// </summary>
    /// <typeparam name="T">The type of the counter value.</typeparam>
    /// <param name="name">The name of the counter.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>The counter, or null if telemetry is not initialized.</returns>
    public static Counter<T> GetOrCreateCounter<T>(string name, string unit = null, string description = null)
        where T : struct
    {
        if (!_isInitialized || _metricsProvider == null)
        {
            return null;
        }

        return _metricsProvider.GetOrCreateCounter<T>(name, unit, description);
    }

    /// <summary>
    /// Gets or creates a histogram with the specified name.
    /// </summary>
    /// <typeparam name="T">The type of the histogram value.</typeparam>
    /// <param name="name">The name of the histogram.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>The histogram, or null if telemetry is not initialized.</returns>
    public static Histogram<T> GetOrCreateHistogram<T>(string name, string unit = null, string description = null)
        where T : struct
    {
        if (!_isInitialized || _metricsProvider == null)
        {
            return null;
        }

        return _metricsProvider.GetOrCreateHistogram<T>(name, unit, description);
    }

    private static void InitializeProviders()
    {
        var resourceBuilder = CreateResourceBuilder();

        // Initialize tracing
        if (_configuration.EnableTracing)
        {
            _activitySourceProvider = new ActivitySourceProvider(
                _configuration.ServiceName,
                _configuration.ServiceVersion);

            _tracerProvider = BuildTracerProvider(resourceBuilder);
        }

        // Initialize metrics
        if (_configuration.EnableMetrics)
        {
            _metricsProvider = new Metrics.MetricsProvider(
                _configuration.ServiceName,
                _configuration.ServiceVersion);

            _otelMeterProvider = BuildMeterProvider(resourceBuilder);
        }

        // Initialize logging
        if (_configuration.EnableLogging)
        {
            _loggerFactory = BuildLoggerFactory(resourceBuilder);
            _loggerProvider = new Logging.LoggerProvider(_loggerFactory, _configuration.ServiceName);
        }

        // Initialize exception tracking
        var logger = _loggerProvider?.Logger ??
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        _exceptionTracker = new ExceptionTracker(
            logger,
            _configuration.ExceptionFilter,
            _configuration.IncludeVisualStudioContext);

        if (_configuration.EnableGlobalExceptionHandler)
        {
            _exceptionTracker.RegisterGlobalExceptionHandler();
        }
    }

    private static ResourceBuilder CreateResourceBuilder()
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: _configuration.ServiceName,
                serviceVersion: _configuration.ServiceVersion)
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("deployment.environment", "visualstudio"),
                new KeyValuePair<string, object>("telemetry.sdk.name", "Otel4Vsix"),
                new KeyValuePair<string, object>("telemetry.sdk.version", "1.0.0")
            });

        // Add custom resource attributes
        if (_configuration.ResourceAttributes.Count > 0)
        {
            var customAttributes = new List<KeyValuePair<string, object>>();
            foreach (var kvp in _configuration.ResourceAttributes)
            {
                customAttributes.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value));
            }
            resourceBuilder.AddAttributes(customAttributes);
        }

        return resourceBuilder;
    }

    private static TracerProvider BuildTracerProvider(ResourceBuilder resourceBuilder)
    {
        var builder = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource(_configuration.ServiceName)
            .SetSampler(new TraceIdRatioBasedSampler(_configuration.TraceSamplingRatio));

        // Add OTLP exporter if endpoint is configured
        if (!string.IsNullOrWhiteSpace(_configuration.OtlpEndpoint))
        {
            builder.AddOtlpExporter(options =>
            {
                ConfigureOtlpExporter(options, OtlpSignalType.Traces);
            });
        }

        // Add console exporter if enabled
        if (_configuration.EnableConsoleExporter)
        {
            builder.AddConsoleExporter();
        }

        return builder.Build();
    }

    private static OpenTelemetry.Metrics.MeterProvider BuildMeterProvider(ResourceBuilder resourceBuilder)
    {
        var builder = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddMeter(_configuration.ServiceName);

        // Add OTLP exporter if endpoint is configured
        if (!string.IsNullOrWhiteSpace(_configuration.OtlpEndpoint))
        {
            builder.AddOtlpExporter(options =>
            {
                ConfigureOtlpExporter(options, OtlpSignalType.Metrics);
            });
        }

        // Add console exporter if enabled
        if (_configuration.EnableConsoleExporter)
        {
            builder.AddConsoleExporter();
        }

        return builder.Build();
    }

    private static ILoggerFactory BuildLoggerFactory(ResourceBuilder resourceBuilder)
    {
        return Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(resourceBuilder);
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;

                // Add OTLP exporter if endpoint is configured
                if (!string.IsNullOrWhiteSpace(_configuration.OtlpEndpoint))
                {
                    options.AddOtlpExporter(exporterOptions =>
                    {
                        ConfigureOtlpExporter(exporterOptions, OtlpSignalType.Logs);
                    });
                }

                // Add console exporter if enabled
                if (_configuration.EnableConsoleExporter)
                {
                    options.AddConsoleExporter();
                }
            });
        });
    }

    private enum OtlpSignalType { Traces, Metrics, Logs }

    private static void ConfigureOtlpExporter(OtlpExporterOptions options, OtlpSignalType signalType = OtlpSignalType.Traces)
    {
        var endpoint = _configuration.OtlpEndpoint.TrimEnd('/');

        // For HTTP protocol, append the signal-specific path if not already present
        if (_configuration.UseOtlpHttp)
        {
            var signalPath = signalType switch
            {
                OtlpSignalType.Traces => "/v1/traces",
                OtlpSignalType.Metrics => "/v1/metrics",
                OtlpSignalType.Logs => "/v1/logs",
                _ => "/v1/traces"
            };

            if (!endpoint.EndsWith(signalPath, StringComparison.OrdinalIgnoreCase))
            {
                endpoint = endpoint + signalPath;
            }
        }

        options.Endpoint = new Uri(endpoint);
        options.Protocol = _configuration.UseOtlpHttp
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;
        options.TimeoutMilliseconds = _configuration.ExportTimeoutMilliseconds;

        // Add custom headers if configured
        if (_configuration.OtlpHeaders.Count > 0)
        {
            var headerString = string.Join(",",
                _configuration.OtlpHeaders.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            options.Headers = headerString;
        }
    }

    private static void ThrowIfNotInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                "VsixTelemetry is not initialized. Call Initialize() first.");
        }
    }
}
