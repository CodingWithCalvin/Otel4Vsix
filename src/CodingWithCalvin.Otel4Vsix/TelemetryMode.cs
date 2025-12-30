namespace CodingWithCalvin.Otel4Vsix;

/// <summary>
/// Specifies the telemetry export mode.
/// </summary>
public enum TelemetryMode
{
    /// <summary>
    /// Automatically determines the mode based on configuration state.
    /// If an OTLP endpoint is configured, uses <see cref="Otlp"/>.
    /// Otherwise, uses <see cref="Debug"/>.
    /// </summary>
    Auto,

    /// <summary>
    /// Telemetry is completely disabled. No data is collected or exported.
    /// </summary>
    Disabled,

    /// <summary>
    /// Telemetry is written to the debug output window via <see cref="System.Diagnostics.Debug"/>.
    /// Useful for local development in Visual Studio.
    /// </summary>
    Debug,

    /// <summary>
    /// Telemetry is exported via OTLP to the configured endpoint.
    /// </summary>
    Otlp
}
