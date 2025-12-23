# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - TBD

### Added

- Initial release
- `VsixTelemetry` static class for easy telemetry access
- `TelemetryConfiguration` for customizing telemetry behavior
- Distributed tracing support via `ActivitySource`
  - `StartActivity` for creating spans
  - `StartCommandActivity` for VS command tracking
  - Pre-configured VS-specific attributes
- Metrics support via `System.Diagnostics.Metrics.Meter`
  - `GetOrCreateCounter<T>` for counters
  - `GetOrCreateHistogram<T>` for histograms
- Structured logging via `Microsoft.Extensions.Logging.ILogger`
  - `Logger` property for default logger
  - `CreateLogger<T>` for typed loggers
- Exception tracking
  - `TrackException` for manual exception recording
  - Global unhandled exception capture via `AppDomain.UnhandledException`
  - Exception filtering support
- OTLP exporter (gRPC and HTTP/protobuf)
- Console exporter for debugging
- Dependency injection support via `AddOtel4Vsix` extension method

[Unreleased]: https://github.com/CodingWithCalvin/Otel4Vsix/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/CodingWithCalvin/Otel4Vsix/releases/tag/v1.0.0
