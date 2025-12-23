using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Otel4Vsix.Extensions
{
    /// <summary>
    /// Extension methods for configuring OpenTelemetry with Microsoft.Extensions.DependencyInjection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Otel4Vsix telemetry services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Action to configure telemetry options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        public static IServiceCollection AddOtel4Vsix(
            this IServiceCollection services,
            Action<TelemetryConfiguration> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var configuration = new TelemetryConfiguration();
            configure?.Invoke(configuration);

            // Initialize the static VsixTelemetry if not already done
            if (!VsixTelemetry.IsInitialized)
            {
                VsixTelemetry.Initialize(configuration);
            }

            // Register services for DI
            services.AddSingleton(configuration);
            services.AddSingleton<ILoggerFactory>(_ => VsixTelemetry.LoggerFactory);
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            return services;
        }

        /// <summary>
        /// Adds Otel4Vsix telemetry services to the service collection with a pre-built configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The telemetry configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services or configuration is null.</exception>
        public static IServiceCollection AddOtel4Vsix(
            this IServiceCollection services,
            TelemetryConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Initialize the static VsixTelemetry if not already done
            if (!VsixTelemetry.IsInitialized)
            {
                VsixTelemetry.Initialize(configuration);
            }

            // Register services for DI
            services.AddSingleton(configuration);
            services.AddSingleton<ILoggerFactory>(_ => VsixTelemetry.LoggerFactory);
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            return services;
        }
    }

    /// <summary>
    /// Generic logger implementation that wraps the VsixTelemetry logger factory.
    /// </summary>
    /// <typeparam name="T">The type to use as the logger category.</typeparam>
    internal sealed class Logger<T> : ILogger<T>
    {
        private readonly ILogger _innerLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger{T}"/> class.
        /// </summary>
        public Logger()
        {
            _innerLogger = VsixTelemetry.CreateLogger<T>() ??
                Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) => _innerLogger.BeginScope(state);

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => _innerLogger.IsEnabled(logLevel);

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            _innerLogger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
