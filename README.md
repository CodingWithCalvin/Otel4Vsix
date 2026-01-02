<p align="center">
  <img src="assets/icon.png" alt="Otel4Vsix Logo" width="128" height="128">
</p>

<h1 align="center">🔭 Otel4Vsix</h1>

<p align="center">
  <a href="https://github.com/CodingWithCalvin/Otel4Vsix/actions/workflows/build.yml"><img src="https://img.shields.io/github/actions/workflow/status/CodingWithCalvin/Otel4Vsix/build.yml?style=for-the-badge&label=Build" alt="Build"></a>
  <a href="https://www.nuget.org/packages/CodingWithCalvin.Otel4Vsix/"><img src="https://img.shields.io/nuget/v/CodingWithCalvin.Otel4Vsix?style=for-the-badge&logo=nuget" alt="NuGet"></a>
  <a href="https://www.nuget.org/packages/CodingWithCalvin.Otel4Vsix/"><img src="https://img.shields.io/nuget/dt/CodingWithCalvin.Otel4Vsix?style=for-the-badge&logo=nuget&label=Downloads" alt="NuGet Downloads"></a>
  <a href="https://opensource.org/licenses/MIT"><img src="https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge" alt="License: MIT"></a>
</p>

<p align="center">
  🚀 <strong>Add OpenTelemetry observability to your Visual Studio extensions in minutes!</strong>
</p>

Otel4Vsix is a powerful yet simple library that brings distributed tracing, metrics, logging, and exception tracking to your VSIX extensions with minimal configuration. See exactly what's happening inside your extension! 👀

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 📊 **Distributed Tracing** | Track operations across your extension with spans and activities |
| 📈 **Metrics** | Counters, histograms, and gauges for performance monitoring |
| 📝 **Structured Logging** | OpenTelemetry-integrated logging via `ILogger` |
| 💥 **Exception Tracking** | Automatic and manual exception capture with full context |
| 🔌 **Multiple Export Modes** | OTLP (gRPC/HTTP) for production, Debug output for development |
| 🎯 **VS-Specific Helpers** | Pre-configured spans for commands, tool windows, and documents |
| 🏗️ **Fluent Builder API** | Clean, chainable configuration |
| 🔧 **Auto-Detection** | Automatically captures VS version, edition, OS, and architecture |

---

## 📦 Installation

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

## 🚀 Quick Start

### 1️⃣ Initialize Telemetry

In your Visual Studio extension's `InitializeAsync` method:

```csharp
using CodingWithCalvin.Otel4Vsix;

protected override async Task InitializeAsync(
    CancellationToken cancellationToken,
    IProgress<ServiceProgressData> progress)
{
    await JoinableTaskFactory.SwitchToMainThreadAsync();

    VsixTelemetry.Configure()
        .WithServiceName("MyAwesomeExtension")
        .WithServiceVersion("1.0.0")
        .WithVisualStudioAttributes(this)  // 🪄 Auto-captures VS version & edition!
        .WithEnvironmentAttributes()        // 🖥️ Auto-captures OS & architecture!
        .WithOtlpHttp("https://api.honeycomb.io")
        .WithHeader("x-honeycomb-team", "your-api-key")
        .Initialize();
}
```

### 2️⃣ Shutdown on Dispose

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

🎉 **That's it!** Your extension is now observable!

---

## 🎛️ Telemetry Modes

Otel4Vsix supports multiple telemetry modes to fit your workflow:

| Mode | Description |
|------|-------------|
| `Auto` | 🤖 **Default** - Uses OTLP if endpoint configured, otherwise Debug output |
| `Debug` | 🐛 Outputs to VS Output window (visible when debugging) |
| `Otlp` | 📡 Exports via OTLP protocol to your collector |
| `Disabled` | 🔇 No telemetry collection |

### 💡 Pro Tip: Development vs Production

```csharp
var builder = VsixTelemetry.Configure()
    .WithServiceName(Vsix.Name)
    .WithServiceVersion(Vsix.Version)
    .WithVisualStudioAttributes(this)
    .WithEnvironmentAttributes();

#if !DEBUG
// 📡 Only send to collector in Release builds
builder
    .WithOtlpHttp("https://api.honeycomb.io")
    .WithHeader("x-honeycomb-team", apiKey);
#endif

builder.Initialize();
```

In Debug builds, telemetry automatically outputs to the VS **Output** window! 🔍

---

## 📊 Usage

### 🔍 Tracing

Create spans to track operations and their duration:

```csharp
// 🎯 Simple span
using var activity = VsixTelemetry.Tracer.StartActivity("ProcessFile");
activity?.SetTag("file.path", filePath);
activity?.SetTag("file.size", fileSize);

// ⚡ VS command span (with pre-configured attributes)
using var commandSpan = VsixTelemetry.StartCommandActivity("MyExtension.DoSomething");

// 🪆 Nested spans for detailed tracing
using var outer = VsixTelemetry.Tracer.StartActivity("LoadProject");
{
    using var inner = VsixTelemetry.Tracer.StartActivity("ParseProjectFile");
    // ... parse logic
}
```

#### ⚠️ Error Handling in Spans

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

### 📈 Metrics

Record counters, histograms, and gauges:

```csharp
// 🔢 Counter - track occurrences
var commandCounter = VsixTelemetry.GetOrCreateCounter<long>(
    "extension.commands.executed",
    "{command}",
    "Number of commands executed");

commandCounter?.Add(1,
    new KeyValuePair<string, object>("command.name", "FormatDocument"));

// 📊 Histogram - track distributions (e.g., durations)
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

### 📝 Logging

Structured logging with OpenTelemetry integration:

```csharp
// 📢 Quick logging methods
VsixTelemetry.LogInformation("Processing file: {FilePath}", filePath);
VsixTelemetry.LogWarning("File not found, using default: {DefaultPath}", defaultPath);
VsixTelemetry.LogError(ex, "Failed to process {FileName}", fileName);

// 🏷️ Create a typed logger for your class
public class MyToolWindow
{
    private readonly ILogger<MyToolWindow> _logger = VsixTelemetry.CreateLogger<MyToolWindow>();

    public void DoWork()
    {
        _logger.LogDebug("Starting work...");
        // ...
        _logger.LogInformation("Work completed successfully! 🎉");
    }
}
```

---

### 💥 Exception Tracking

Track exceptions with full context:

```csharp
// 🎯 Manual exception tracking
try
{
    // risky operation
}
catch (Exception ex)
{
    VsixTelemetry.TrackException(ex);
    // Handle or rethrow
}

// 📋 With additional context
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

> 💡 **Note**: Global unhandled exceptions are automatically captured when `EnableGlobalExceptionHandler` is `true` (default).

---

## ⚙️ Configuration Options

### 🏗️ Fluent Builder Methods

| Method | Description |
|--------|-------------|
| `WithServiceName(name)` | Set the service name for identification |
| `WithServiceVersion(version)` | Set the service version |
| `WithVisualStudioAttributes(serviceProvider)` | 🪄 Auto-capture VS version & edition |
| `WithVisualStudioAttributes(version, edition)` | Manually set VS attributes |
| `WithEnvironmentAttributes()` | 🖥️ Auto-capture OS version & architecture |
| `WithResourceAttribute(key, value)` | Add custom resource attributes |
| `WithOtlpHttp(endpoint)` | Configure OTLP HTTP export |
| `WithOtlpGrpc(endpoint)` | Configure OTLP gRPC export |
| `WithHeader(key, value)` | Add headers for OTLP requests |
| `WithMode(mode)` | Set telemetry mode (Auto/Debug/Otlp/Disabled) |
| `WithTracing(enabled)` | Enable/disable tracing |
| `WithMetrics(enabled)` | Enable/disable metrics |
| `WithLogging(enabled)` | Enable/disable logging |
| `WithTraceSamplingRatio(ratio)` | Set trace sampling (0.0 - 1.0) |
| `WithGlobalExceptionHandler(enabled)` | Enable/disable auto exception capture |
| `WithExceptionFilter(filter)` | Filter which exceptions to track |
| `WithExportTimeout(ms)` | Set export timeout in milliseconds |
| `Initialize()` | 🚀 Initialize telemetry |

### 📋 Auto-Captured Attributes

When using the helper methods, these attributes are automatically captured:

| Attribute | Source | Example |
|-----------|--------|---------|
| `vs.version` | `WithVisualStudioAttributes()` | `"17.12.35521.163"` |
| `vs.edition` | `WithVisualStudioAttributes()` | `"Enterprise"` |
| `os.version` | `WithEnvironmentAttributes()` | `"10.0.22631.0"` |
| `host.arch` | `WithEnvironmentAttributes()` | `"X64"` or `"Arm64"` |

---

## 🔌 Supported Backends

Otel4Vsix exports telemetry via OTLP, which is supported by:

| Backend | Link |
|---------|------|
| 🐝 Honeycomb | [honeycomb.io](https://www.honeycomb.io/) |
| 🔵 Azure Monitor | [Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-overview) |
| 🐕 Datadog | [datadoghq.com](https://www.datadoghq.com/) |
| 🟡 Jaeger | [jaegertracing.io](https://www.jaegertracing.io/) |
| 🔴 Grafana Tempo | [grafana.com/oss/tempo](https://grafana.com/oss/tempo/) |
| 📮 Zipkin | [zipkin.io](https://zipkin.io/) |
| ☁️ AWS X-Ray | [aws.amazon.com/xray](https://aws.amazon.com/xray/) |
| 🌐 Google Cloud Trace | [cloud.google.com/trace](https://cloud.google.com/trace) |
| 🔧 Any OTLP-compatible collector | — |

---

## 📋 Example: Full Production Setup

```csharp
using CodingWithCalvin.Otel4Vsix;

public sealed class MyExtensionPackage : AsyncPackage
{
    protected override async Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync();

        var builder = VsixTelemetry.Configure()
            .WithServiceName("MyExtension")
            .WithServiceVersion(Vsix.Version)
            .WithVisualStudioAttributes(this)
            .WithEnvironmentAttributes()
            .WithResourceAttribute("deployment.environment", "production")
            .WithTraceSamplingRatio(0.1)  // Sample 10% of traces
            .WithExceptionFilter(ex => ex is not OperationCanceledException);

#if !DEBUG
        builder
            .WithOtlpHttp("https://api.honeycomb.io")
            .WithHeader("x-honeycomb-team", Config.HoneycombApiKey);
#endif

        builder.Initialize();

        // ... rest of initialization
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            VsixTelemetry.Shutdown();
        }
        base.Dispose(disposing);
    }
}
```

---

## 📋 Requirements

| Requirement | Version |
|-------------|---------|
| .NET Framework | 4.8 |
| Visual Studio | 2022 or later |

---

## 🤝 Contributing

Contributions are welcome! 🎉

1. 🍴 Fork the repository
2. 🌿 Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. 💾 Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. 📤 Push to the branch (`git push origin feature/AmazingFeature`)
5. 🔃 Open a Pull Request

---

## 👥 Contributors

<!-- readme: contributors -start -->
[![CalvinAllen](https://avatars.githubusercontent.com/u/41448698?v=4&s=64)](https://github.com/CalvinAllen) 
<!-- readme: contributors -end -->

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Built on top of [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet) 🔭
- Inspired by the need for better observability in Visual Studio extensions 💡
- Made with ❤️ by [Coding with Calvin](https://github.com/CodingWithCalvin)
