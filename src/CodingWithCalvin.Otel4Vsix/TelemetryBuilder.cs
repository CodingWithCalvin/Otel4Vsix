using System;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodingWithCalvin.Otel4Vsix;

/// <summary>
/// Fluent builder for configuring and initializing OpenTelemetry in Visual Studio extensions.
/// </summary>
public sealed class TelemetryBuilder
{
    private readonly TelemetryConfiguration _configuration = new TelemetryConfiguration();

    /// <summary>
    /// Sets the service name used to identify this extension in telemetry.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithServiceName(string serviceName)
    {
        _configuration.ServiceName = serviceName;
        return this;
    }

    /// <summary>
    /// Sets the service version.
    /// </summary>
    /// <param name="serviceVersion">The service version.</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithServiceVersion(string serviceVersion)
    {
        _configuration.ServiceVersion = serviceVersion;
        return this;
    }

    /// <summary>
    /// Configures OTLP export using HTTP/protobuf protocol.
    /// </summary>
    /// <param name="endpoint">The OTLP HTTP endpoint (e.g., "https://api.honeycomb.io").</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithOtlpHttp(string endpoint)
    {
        _configuration.OtlpEndpoint = endpoint;
        _configuration.UseOtlpHttp = true;
        return this;
    }

    /// <summary>
    /// Configures OTLP export using gRPC protocol.
    /// </summary>
    /// <param name="endpoint">The OTLP gRPC endpoint (e.g., "http://localhost:4317").</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithOtlpGrpc(string endpoint)
    {
        _configuration.OtlpEndpoint = endpoint;
        _configuration.UseOtlpHttp = false;
        return this;
    }

    /// <summary>
    /// Adds a header to OTLP export requests.
    /// </summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithHeader(string key, string value)
    {
        _configuration.OtlpHeaders[key] = value;
        return this;
    }

    /// <summary>
    /// Adds a resource attribute to all telemetry.
    /// </summary>
    /// <param name="key">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithResourceAttribute(string key, object value)
    {
        _configuration.ResourceAttributes[key] = value;
        return this;
    }

    /// <summary>
    /// Adds Visual Studio version and edition as resource attributes by querying VS services.
    /// </summary>
    /// <param name="serviceProvider">The async service provider (typically the AsyncPackage).</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Adds the following resource attributes:
    /// <list type="bullet">
    /// <item><c>vs.version</c> - The full Visual Studio version (e.g., "17.12.35521.163")</item>
    /// <item><c>vs.edition</c> - The Visual Studio edition (e.g., "Community", "Professional", "Enterprise")</item>
    /// </list>
    /// This method queries DTE for the edition and IVsShell for the full release version.
    /// </remarks>
    public TelemetryBuilder WithVisualStudioAttributes(IAsyncServiceProvider serviceProvider)
    {
        ThreadHelper.JoinableTaskFactory.Run(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var vsEdition = "unknown";
            var vsVersion = "unknown";

            var dte = await serviceProvider.GetServiceAsync(typeof(DTE)) as DTE2;
            if (dte != null)
            {
                vsEdition = dte.Edition ?? "unknown";
            }

            var shell = await serviceProvider.GetServiceAsync(typeof(SVsShell)) as IVsShell;
            if (shell != null && shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out var versionObj) == 0)
            {
                vsVersion = versionObj as string ?? "unknown";
            }

            _configuration.ResourceAttributes["vs.version"] = vsVersion;
            _configuration.ResourceAttributes["vs.edition"] = vsEdition;
        });

        return this;
    }

    /// <summary>
    /// Adds Visual Studio version and edition as resource attributes.
    /// </summary>
    /// <param name="version">The Visual Studio version (e.g., "17.12").</param>
    /// <param name="edition">The Visual Studio edition (e.g., "Community", "Professional", "Enterprise").</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Adds the following resource attributes:
    /// <list type="bullet">
    /// <item><c>vs.version</c> - The Visual Studio version</item>
    /// <item><c>vs.edition</c> - The Visual Studio edition</item>
    /// </list>
    /// </remarks>
    public TelemetryBuilder WithVisualStudioAttributes(string version, string edition)
    {
        _configuration.ResourceAttributes["vs.version"] = version ?? "unknown";
        _configuration.ResourceAttributes["vs.edition"] = edition ?? "unknown";
        return this;
    }

    /// <summary>
    /// Adds environment attributes for OS and architecture.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Adds the following resource attributes:
    /// <list type="bullet">
    /// <item><c>os.version</c> - The operating system version</item>
    /// <item><c>host.arch</c> - The processor architecture (e.g., "X64", "Arm64")</item>
    /// </list>
    /// </remarks>
    public TelemetryBuilder WithEnvironmentAttributes()
    {
        _configuration.ResourceAttributes["os.version"] = Environment.OSVersion.Version.ToString();
        _configuration.ResourceAttributes["host.arch"] = RuntimeInformation.ProcessArchitecture.ToString();
        return this;
    }

    /// <summary>
    /// Sets the telemetry mode.
    /// </summary>
    /// <param name="mode">The telemetry mode. Defaults to <see cref="TelemetryMode.Auto"/>.</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithMode(TelemetryMode mode)
    {
        _configuration.Mode = mode;
        return this;
    }

    /// <summary>
    /// Enables or disables tracing.
    /// </summary>
    /// <param name="enabled">True to enable tracing, false to disable.</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithTracing(bool enabled = true)
    {
        _configuration.EnableTracing = enabled;
        return this;
    }

    /// <summary>
    /// Enables or disables metrics collection.
    /// </summary>
    /// <param name="enabled">True to enable metrics, false to disable.</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithMetrics(bool enabled = true)
    {
        _configuration.EnableMetrics = enabled;
        return this;
    }

    /// <summary>
    /// Enables or disables logging.
    /// </summary>
    /// <param name="enabled">True to enable logging, false to disable.</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithLogging(bool enabled = true)
    {
        _configuration.EnableLogging = enabled;
        return this;
    }

    /// <summary>
    /// Sets the trace sampling ratio.
    /// </summary>
    /// <param name="ratio">The sampling ratio (0.0 to 1.0). 1.0 samples all traces.</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithTraceSamplingRatio(double ratio)
    {
        _configuration.TraceSamplingRatio = ratio;
        return this;
    }

    /// <summary>
    /// Enables or disables the global exception handler.
    /// </summary>
    /// <param name="enabled">True to enable, false to disable.</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithGlobalExceptionHandler(bool enabled = true)
    {
        _configuration.EnableGlobalExceptionHandler = enabled;
        return this;
    }

    /// <summary>
    /// Sets an exception filter function.
    /// </summary>
    /// <param name="filter">Function that returns true to track the exception, false to ignore.</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithExceptionFilter(Func<Exception, bool> filter)
    {
        _configuration.ExceptionFilter = filter;
        return this;
    }

    /// <summary>
    /// Enables or disables Visual Studio context in telemetry.
    /// </summary>
    /// <param name="enabled">True to include VS context, false to exclude.</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithVisualStudioContext(bool enabled = true)
    {
        _configuration.IncludeVisualStudioContext = enabled;
        return this;
    }

    /// <summary>
    /// Sets the export timeout.
    /// </summary>
    /// <param name="timeoutMilliseconds">The timeout in milliseconds.</param>
    /// <returns>The builder for chaining.</returns>
    public TelemetryBuilder WithExportTimeout(int timeoutMilliseconds)
    {
        _configuration.ExportTimeoutMilliseconds = timeoutMilliseconds;
        return this;
    }

    /// <summary>
    /// Builds the configuration and initializes telemetry.
    /// </summary>
    /// <remarks>
    /// When <see cref="TelemetryMode.Auto"/> is set (the default), the effective mode is
    /// determined by inspecting the configuration:
    /// <list type="bullet">
    /// <item>If an OTLP endpoint is configured, telemetry is exported via OTLP.</item>
    /// <item>Otherwise, telemetry is written to the debug output window.</item>
    /// </list>
    /// </remarks>
    public void Initialize()
    {
        VsixTelemetry.Initialize(_configuration);
    }

    /// <summary>
    /// Builds and returns the configuration without initializing.
    /// </summary>
    /// <returns>The built configuration.</returns>
    public TelemetryConfiguration Build()
    {
        return _configuration;
    }
}
