# Otel4Vsix

[![Build](https://github.com/CodingWithCalvin/Otel4Vsix/actions/workflows/build.yml/badge.svg)](https://github.com/CodingWithCalvin/Otel4Vsix/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/CodingWithCalvin.Otel4Vsix.svg)](https://www.nuget.org/packages/CodingWithCalvin.Otel4Vsix/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CodingWithCalvin.Otel4Vsix.svg)](https://www.nuget.org/packages/CodingWithCalvin.Otel4Vsix/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

OpenTelemetry support library for Visual Studio 2022+ extensions. Add distributed tracing, metrics, logging, and exception tracking to your VSIX with minimal configuration.

---

## Features

- **Distributed Tracing** - Track operations across your extension with spans and activities
- **Metrics** - Counters, histograms, and gauges for performance monitoring
- **Structured Logging** - OpenTelemetry-integrated logging via `ILogger`
- **Exception Tracking** - Automatic and manual exception capture with full context
- **Multiple Exporters** - OTLP (gRPC/HTTP) for production, Console for debugging
- **VS-Specific Helpers** - Pre-configured spans for commands, tool windows, and documents

---

## Installation

### Package Manager
```powershell
Install-Package CodingWithCalvin.Otel4Vsix
```

### .NET CLI
```bash
dotnet add package CodingWithCalvin.Otel4Vsix
```

### PackageReference
```xml
<PackageReference Include="CodingWithCalvin.Otel4Vsix" Version="1.0.0" />
```

---

## Quick Start

### 1. Initialize Telemetry

In your Visual Studio extension's `InitializeAsync` method:

```csharp
using Otel4Vsix;

protected override async Task InitializeAsync(
    CancellationToken cancellationToken,
    IProgress<ServiceProgressData> progress)
{
    await base.InitializeAsync(cancellationToken, progress);

    VsixTelemetry.Initialize(new TelemetryConfiguration
    {
        ServiceName = "MyAwesomeExtension",
        ServiceVersion = "1.0.0",
        OtlpEndpoint = "http://localhost:4317",
        EnableConsoleExporter = true  // Useful during development
    });
}
```

### 2. Shutdown on Dispose

```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        VsixTelemetry.Shutdown();
    }
    base.Dispose(disposing);
}
```

---

## Usage

### Tracing

Create spans to track operations and their duration:

```csharp
// Simple span
using var activity = VsixTelemetry.Tracer.StartActivity("ProcessFile");
activity?.SetTag("file.path", filePath);
activity?.SetTag("file.size", fileSize);

// VS command span (with pre-configured attributes)
using var commandSpan = VsixTelemetry.StartCommandActivity("MyExtension.DoSomething");

// Nested spans for detailed tracing
using var outer = VsixTelemetry.Tracer.StartActivity("LoadProject");
{
    using var inner = VsixTelemetry.Tracer.StartActivity("ParseProjectFile");
    // ... parse logic
}
```

#### Error Handling in Spans

```csharp
using var activity = VsixTelemetry.StartActivity("RiskyOperation");
try
{
    // Your code here
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.RecordException(ex);
    throw;
}
```

---

### Metrics

Record counters, histograms, and gauges:

```csharp
// Counter - track occurrences
var commandCounter = VsixTelemetry.GetOrCreateCounter<long>(
    "extension.commands.executed",
    "{command}",
    "Number of commands executed");

commandCounter?.Add(1,
    new KeyValuePair<string, object>("command.name", "FormatDocument"));

// Histogram - track distributions (e.g., durations)
var durationHistogram = VsixTelemetry.GetOrCreateHistogram<double>(
    "extension.operation.duration",
    "ms",
    "Duration of operations in milliseconds");

var stopwatch = Stopwatch.StartNew();
// ... do work ...
stopwatch.Stop();
durationHistogram?.Record(stopwatch.ElapsedMilliseconds,
    new KeyValuePair<string, object>("operation.name", "BuildSolution"));
```

---

### Logging

Structured logging with OpenTelemetry integration:

```csharp
// Use the default logger
VsixTelemetry.Logger.LogInformation("Processing file: {FilePath}", filePath);
VsixTelemetry.Logger.LogWarning("File not found, using default: {DefaultPath}", defaultPath);

// Create a typed logger for your class
public class MyToolWindow
{
    private readonly ILogger<MyToolWindow> _logger = VsixTelemetry.CreateLogger<MyToolWindow>();

    public void DoWork()
    {
        _logger.LogDebug("Starting work...");
        // ...
        _logger.LogInformation("Work completed successfully");
    }
}

// Log errors with exceptions
try
{
    // risky operation
}
catch (Exception ex)
{
    VsixTelemetry.Logger.LogError(ex, "Failed to process {FileName}", fileName);
}
```

---

### Exception Tracking

Track exceptions with full context:

```csharp
// Manual exception tracking
try
{
    // risky operation
}
catch (Exception ex)
{
    VsixTelemetry.TrackException(ex);
    // Handle or rethrow
}

// With additional context
catch (Exception ex)
{
    VsixTelemetry.TrackException(ex, new Dictionary<string, object>
    {
        { "operation.name", "LoadProject" },
        { "project.path", projectPath },
        { "user.action", "OpenSolution" }
    });
    throw;
}
```

> **Note**: Global unhandled exceptions are automatically captured when `EnableGlobalExceptionHandler` is `true` (default).

---

## Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ServiceName` | `string` | `"VsixExtension"` | Service name for telemetry identification |
| `ServiceVersion` | `string` | `"1.0.0"` | Service version |
| `OtlpEndpoint` | `string` | `null` | OTLP collector endpoint (e.g., `http://localhost:4317`) |
| `UseOtlpHttp` | `bool` | `false` | Use HTTP/protobuf instead of gRPC |
| `OtlpHeaders` | `IDictionary<string, string>` | empty | Custom headers for OTLP requests (auth, API keys) |
| `EnableConsoleExporter` | `bool` | `false` | Output telemetry to console (for debugging) |
| `EnableTracing` | `bool` | `true` | Enable distributed tracing |
| `EnableMetrics` | `bool` | `true` | Enable metrics collection |
| `EnableLogging` | `bool` | `true` | Enable structured logging |
| `EnableGlobalExceptionHandler` | `bool` | `true` | Capture unhandled exceptions automatically |
| `TraceSamplingRatio` | `double` | `1.0` | Trace sampling ratio (`0.0` - `1.0`) |
| `IncludeVisualStudioContext` | `bool` | `true` | Add VS context to telemetry |
| `ExceptionFilter` | `Func<Exception, bool>` | `null` | Filter which exceptions to track |
| `ResourceAttributes` | `IDictionary<string, object>` | empty | Custom resource attributes |
| `ExportTimeoutMilliseconds` | `int` | `30000` | Export timeout |
| `BatchExportScheduledDelayMilliseconds` | `int` | `5000` | Batch export delay |

### Example: Production Configuration

```csharp
VsixTelemetry.Initialize(new TelemetryConfiguration
{
    ServiceName = "MyExtension",
    ServiceVersion = typeof(MyPackage).Assembly.GetName().Version.ToString(),
    OtlpEndpoint = "https://otel-collector.mycompany.com:4317",
    TraceSamplingRatio = 0.1,  // Sample 10% of traces
    EnableConsoleExporter = false,
    ResourceAttributes =
    {
        { "deployment.environment", "production" },
        { "service.namespace", "visualstudio-extensions" }
    },
    ExceptionFilter = ex => !(ex is OperationCanceledException)  // Ignore cancellations
});
```

### Example: Using Custom Headers (Honeycomb, etc.)

```csharp
var config = new TelemetryConfiguration
{
    ServiceName = "MyExtension",
    OtlpEndpoint = "https://api.honeycomb.io:443",
    UseOtlpHttp = true
};

// Add authentication headers
config.OtlpHeaders["x-honeycomb-team"] = "your-api-key";
config.OtlpHeaders["x-honeycomb-dataset"] = "your-dataset";

VsixTelemetry.Initialize(config);
```

---

## Dependency Injection

If your extension uses dependency injection:

```csharp
using Otel4Vsix.Extensions;

public void ConfigureServices(IServiceCollection services)
{
    services.AddOtel4Vsix(config =>
    {
        config.ServiceName = "MyExtension";
        config.OtlpEndpoint = "http://localhost:4317";
    });

    // Now ILogger<T> is available via DI
}
```

---

## Supported Backends

Otel4Vsix exports telemetry via OTLP, which is supported by:

- [Jaeger](https://www.jaegertracing.io/)
- [Zipkin](https://zipkin.io/)
- [Azure Monitor / Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-overview)
- [Honeycomb](https://www.honeycomb.io/)
- [Datadog](https://www.datadoghq.com/)
- [Grafana Tempo](https://grafana.com/oss/tempo/)
- [AWS X-Ray](https://aws.amazon.com/xray/)
- [Google Cloud Trace](https://cloud.google.com/trace)
- Any OTLP-compatible collector

---

## Requirements

- **.NET Framework 4.8**
- **Visual Studio 2022** or later

## Dependencies

- OpenTelemetry (>= 1.7.0)
- OpenTelemetry.Exporter.OpenTelemetryProtocol (>= 1.7.0)
- OpenTelemetry.Exporter.Console (>= 1.7.0)
- Microsoft.Extensions.Logging (>= 8.0.0)
- Microsoft.VisualStudio.SDK (>= 17.0)

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Acknowledgments

- Built on top of [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
- Inspired by the need for better observability in Visual Studio extensions
