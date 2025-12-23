using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace Otel4Vsix.Metrics
{
    /// <summary>
    /// Provides and manages the <see cref="Meter"/> for creating metrics.
    /// </summary>
    internal sealed class MetricsProvider : IDisposable
    {
        private readonly Meter _meter;
        private readonly Dictionary<string, object> _instruments;
        private readonly object _lock = new object();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsProvider"/> class.
        /// </summary>
        /// <param name="serviceName">The name of the service for metric identification.</param>
        /// <param name="serviceVersion">The version of the service.</param>
        /// <exception cref="ArgumentNullException">Thrown when serviceName is null.</exception>
        public MetricsProvider(string serviceName, string serviceVersion)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            _meter = new Meter(serviceName, serviceVersion ?? "1.0.0");
            _instruments = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the <see cref="Meter"/> for creating metrics instruments.
        /// </summary>
        public Meter Meter
        {
            get
            {
                ThrowIfDisposed();
                return _meter;
            }
        }

        /// <summary>
        /// Creates or retrieves a counter with the specified name.
        /// </summary>
        /// <typeparam name="T">The type of the counter value.</typeparam>
        /// <param name="name">The name of the counter.</param>
        /// <param name="unit">Optional unit of measurement.</param>
        /// <param name="description">Optional description of the counter.</param>
        /// <returns>The counter instrument.</returns>
        public Counter<T> GetOrCreateCounter<T>(string name, string unit = null, string description = null)
            where T : struct
        {
            ThrowIfDisposed();
            lock (_lock)
            {
                if (_instruments.TryGetValue(name, out var existing) && existing is Counter<T> counter)
                {
                    return counter;
                }

                var newCounter = _meter.CreateCounter<T>(name, unit, description);
                _instruments[name] = newCounter;
                return newCounter;
            }
        }

        /// <summary>
        /// Creates or retrieves a histogram with the specified name.
        /// </summary>
        /// <typeparam name="T">The type of the histogram value.</typeparam>
        /// <param name="name">The name of the histogram.</param>
        /// <param name="unit">Optional unit of measurement.</param>
        /// <param name="description">Optional description of the histogram.</param>
        /// <returns>The histogram instrument.</returns>
        public Histogram<T> GetOrCreateHistogram<T>(string name, string unit = null, string description = null)
            where T : struct
        {
            ThrowIfDisposed();
            lock (_lock)
            {
                if (_instruments.TryGetValue(name, out var existing) && existing is Histogram<T> histogram)
                {
                    return histogram;
                }

                var newHistogram = _meter.CreateHistogram<T>(name, unit, description);
                _instruments[name] = newHistogram;
                return newHistogram;
            }
        }

        /// <summary>
        /// Creates or retrieves an up-down counter with the specified name.
        /// </summary>
        /// <typeparam name="T">The type of the counter value.</typeparam>
        /// <param name="name">The name of the counter.</param>
        /// <param name="unit">Optional unit of measurement.</param>
        /// <param name="description">Optional description of the counter.</param>
        /// <returns>The up-down counter instrument.</returns>
        public UpDownCounter<T> GetOrCreateUpDownCounter<T>(string name, string unit = null, string description = null)
            where T : struct
        {
            ThrowIfDisposed();
            lock (_lock)
            {
                if (_instruments.TryGetValue(name, out var existing) && existing is UpDownCounter<T> counter)
                {
                    return counter;
                }

                var newCounter = _meter.CreateUpDownCounter<T>(name, unit, description);
                _instruments[name] = newCounter;
                return newCounter;
            }
        }

        /// <summary>
        /// Creates a pre-configured counter for tracking command executions.
        /// </summary>
        /// <returns>A counter for command executions.</returns>
        public Counter<long> CreateCommandExecutionCounter()
        {
            return GetOrCreateCounter<long>(
                "vs.extension.commands.executed",
                "{command}",
                "Number of commands executed");
        }

        /// <summary>
        /// Creates a pre-configured histogram for tracking operation durations.
        /// </summary>
        /// <returns>A histogram for operation durations.</returns>
        public Histogram<double> CreateOperationDurationHistogram()
        {
            return GetOrCreateHistogram<double>(
                "vs.extension.operation.duration",
                "ms",
                "Duration of operations in milliseconds");
        }

        /// <summary>
        /// Creates a pre-configured counter for tracking errors.
        /// </summary>
        /// <returns>A counter for errors.</returns>
        public Counter<long> CreateErrorCounter()
        {
            return GetOrCreateCounter<long>(
                "vs.extension.errors",
                "{error}",
                "Number of errors encountered");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _meter?.Dispose();
                _instruments.Clear();
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MetricsProvider));
            }
        }
    }
}
