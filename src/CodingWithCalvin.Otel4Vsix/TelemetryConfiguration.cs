using System;
using System.Collections.Generic;

namespace Otel4Vsix
{
    /// <summary>
    /// Configuration options for OpenTelemetry in Visual Studio extensions.
    /// </summary>
    public sealed class TelemetryConfiguration
    {
        /// <summary>
        /// Gets or sets the service name used to identify this extension in telemetry.
        /// </summary>
        /// <remarks>
        /// This name appears in traces, metrics, and logs as the service identifier.
        /// Defaults to "VsixExtension" if not specified.
        /// </remarks>
        public string ServiceName { get; set; } = "VsixExtension";

        /// <summary>
        /// Gets or sets the service version.
        /// </summary>
        public string ServiceVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the OTLP endpoint URL for exporting telemetry.
        /// </summary>
        /// <remarks>
        /// Use gRPC endpoint (default port 4317) or HTTP endpoint (default port 4318).
        /// Example: "http://localhost:4317"
        /// </remarks>
        public string OtlpEndpoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use HTTP/protobuf instead of gRPC for OTLP export.
        /// </summary>
        /// <remarks>
        /// Set to true if your collector only supports HTTP/protobuf (e.g., some cloud providers).
        /// Default is false (uses gRPC).
        /// </remarks>
        public bool UseOtlpHttp { get; set; }

        /// <summary>
        /// Gets custom headers to include in OTLP export requests.
        /// </summary>
        /// <remarks>
        /// Use this to add authentication headers (API keys, bearer tokens) or other custom headers
        /// required by your telemetry backend.
        /// <example>
        /// <code>
        /// config.OtlpHeaders["x-api-key"] = "your-api-key";
        /// config.OtlpHeaders["Authorization"] = "Bearer your-token";
        /// </code>
        /// </example>
        /// </remarks>
        public IDictionary<string, string> OtlpHeaders { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets a value indicating whether the console exporter is enabled.
        /// </summary>
        /// <remarks>
        /// Useful for debugging during development. Outputs telemetry to the console/debug output.
        /// </remarks>
        public bool EnableConsoleExporter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tracing is enabled.
        /// </summary>
        public bool EnableTracing { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether metrics collection is enabled.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether logging is enabled.
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to capture unhandled exceptions globally.
        /// </summary>
        /// <remarks>
        /// When enabled, registers handlers for <see cref="AppDomain.UnhandledException"/>
        /// to automatically record exceptions in telemetry.
        /// </remarks>
        public bool EnableGlobalExceptionHandler { get; set; } = true;

        /// <summary>
        /// Gets or sets the sampling ratio for traces (0.0 to 1.0).
        /// </summary>
        /// <remarks>
        /// A value of 1.0 means all traces are sampled. A value of 0.5 means 50% of traces are sampled.
        /// Default is 1.0 (sample all traces).
        /// </remarks>
        public double TraceSamplingRatio { get; set; } = 1.0;

        /// <summary>
        /// Gets additional resource attributes to include in all telemetry.
        /// </summary>
        /// <remarks>
        /// These attributes are added to the OpenTelemetry resource and appear on all telemetry items.
        /// Common attributes include deployment environment, instance ID, etc.
        /// </remarks>
        public IDictionary<string, object> ResourceAttributes { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets an optional filter function for exceptions.
        /// </summary>
        /// <remarks>
        /// Return true to track the exception, false to ignore it.
        /// If null, all exceptions are tracked.
        /// </remarks>
        public Func<Exception, bool> ExceptionFilter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include Visual Studio context in telemetry.
        /// </summary>
        /// <remarks>
        /// When enabled, adds VS-specific attributes like active document, solution name, etc.
        /// </remarks>
        public bool IncludeVisualStudioContext { get; set; } = true;

        /// <summary>
        /// Gets or sets the export timeout in milliseconds.
        /// </summary>
        public int ExportTimeoutMilliseconds { get; set; } = 30000;

        /// <summary>
        /// Gets or sets the batch export scheduled delay in milliseconds.
        /// </summary>
        public int BatchExportScheduledDelayMilliseconds { get; set; } = 5000;

        /// <summary>
        /// Validates the configuration and throws if invalid.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                throw new ArgumentException("ServiceName cannot be null or empty.", nameof(ServiceName));
            }

            if (TraceSamplingRatio < 0.0 || TraceSamplingRatio > 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(TraceSamplingRatio),
                    TraceSamplingRatio,
                    "TraceSamplingRatio must be between 0.0 and 1.0.");
            }

            if (ExportTimeoutMilliseconds <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ExportTimeoutMilliseconds),
                    ExportTimeoutMilliseconds,
                    "ExportTimeoutMilliseconds must be greater than 0.");
            }

            if (BatchExportScheduledDelayMilliseconds <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(BatchExportScheduledDelayMilliseconds),
                    BatchExportScheduledDelayMilliseconds,
                    "BatchExportScheduledDelayMilliseconds must be greater than 0.");
            }
        }
    }
}
