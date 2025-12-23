# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Critical Rules

**These rules override all other instructions:**

1. **NEVER commit directly to main** - Always create a feature branch and submit a pull request
2. **Conventional commits** - Format: `type(scope): description`
3. **GitHub Issues for TODOs** - Use `gh` CLI to manage issues, no local TODO files. Use conventional commit format for issue titles
4. **Pull Request titles** - Use conventional commit format (same as commits)
5. **Branch naming** - Use format: `type/scope/short-description` (e.g., `feat/tracing/add-batch-processor`)
6. **Working an issue** - Always create a new branch from an updated main branch
7. **Check branch status before pushing** - Verify the remote tracking branch still exists. If a PR was merged/deleted, create a new branch from main instead
8. **Microsoft coding guidelines** - Follow Microsoft C# coding conventions and .NET library design guidelines

---

### GitHub CLI Commands

```bash
gh issue list                    # List open issues
gh issue view <number>           # View details
gh issue create --title "type(scope): description" --body "..."
gh issue close <number>
```

### Conventional Commit Types

| Type | Description |
|------|-------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `test` | Adding or updating tests |
| `chore` | Maintenance tasks |
| `perf` | Performance improvement |

---

## Project Overview

Otel4Vsix is a .NET Framework 4.8 library that provides OpenTelemetry support for Visual Studio 2022+ extensions. It offers tracing, metrics, logging, and exception tracking capabilities with minimal configuration.

**NuGet Package:** `CodingWithCalvin.Otel4Vsix`

## Build Commands

```bash
# Restore and build
dotnet build CodingWithCalvin.Otel4Vsix.slnx

# Build Release
dotnet build CodingWithCalvin.Otel4Vsix.slnx --configuration Release

# Create NuGet package
dotnet pack src/CodingWithCalvin.Otel4Vsix/CodingWithCalvin.Otel4Vsix.csproj --configuration Release --output ./nupkg
```

## Architecture

The library follows a static facade pattern for ease of use:

- **VsixTelemetry.cs** - Main static entry point providing `Tracer`, `Meter`, `Logger`, and exception tracking
- **TelemetryConfiguration.cs** - Configuration options (service name, OTLP endpoint, exporters, etc.)
- **Tracing/ActivitySourceProvider.cs** - Manages `ActivitySource` for creating spans
- **Metrics/MetricsProvider.cs** - Manages `Meter` for counters, histograms, gauges
- **Logging/LoggerProvider.cs** - Wraps `ILoggerFactory` for OpenTelemetry-integrated logging
- **Exceptions/ExceptionTracker.cs** - Captures exceptions manually and via global handlers

## Key Implementation Details

- Thread-safe static initialization via `VsixTelemetry.Initialize()`
- Conditional exporters: OTLP (gRPC/HTTP) and Console
- Global exception handler hooks into `AppDomain.UnhandledException`
- SourceLink enabled for debugging into NuGet source
- All public APIs have XML documentation

## Dependencies

- OpenTelemetry (>= 1.7.0)
- OpenTelemetry.Exporter.OpenTelemetryProtocol
- OpenTelemetry.Exporter.Console
- Microsoft.Extensions.Logging
- Microsoft.VisualStudio.SDK (>= 17.0)

## Release Process

1. Merge PRs to main
2. Push a version tag: `git tag v1.0.0 && git push origin v1.0.0`
3. GitHub Action automatically:
   - Builds and packs with that version
   - Creates GitHub release with changelog
   - Publishes to NuGet.org
