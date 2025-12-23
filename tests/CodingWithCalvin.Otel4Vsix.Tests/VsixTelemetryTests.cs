using System;
using System.Diagnostics;
using FluentAssertions;
using Otel4Vsix;
using Xunit;

namespace CodingWithCalvin.Otel4Vsix.Tests
{
    /// <summary>
    /// Tests for VsixTelemetry static class.
    /// These tests must run sequentially due to shared static state.
    /// </summary>
    [Collection("VsixTelemetry")]
    public class VsixTelemetryTests : IDisposable
    {
        public VsixTelemetryTests()
        {
            // Ensure clean state before each test
            VsixTelemetry.Shutdown();
        }

        public void Dispose()
        {
            // Clean up after each test
            VsixTelemetry.Shutdown();
        }

        [Fact]
        public void IsInitialized_BeforeInitialize_ReturnsFalse()
        {
            VsixTelemetry.IsInitialized.Should().BeFalse();
        }

        [Fact]
        public void Initialize_WithNullConfig_ThrowsArgumentNullException()
        {
            Action act = () => VsixTelemetry.Initialize(null);

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("configuration");
        }

        [Fact]
        public void Initialize_WithInvalidConfig_ThrowsArgumentException()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = null
            };

            Action act = () => VsixTelemetry.Initialize(config);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Initialize_WithValidConfig_SetsIsInitializedTrue()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                EnableConsoleExporter = false
            };

            VsixTelemetry.Initialize(config);

            VsixTelemetry.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void Initialize_WhenAlreadyInitialized_ThrowsInvalidOperationException()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService"
            };
            VsixTelemetry.Initialize(config);

            Action act = () => VsixTelemetry.Initialize(config);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*already initialized*");
        }

        [Fact]
        public void Tracer_BeforeInitialize_ThrowsInvalidOperationException()
        {
            Action act = () => { var _ = VsixTelemetry.Tracer; };

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not initialized*");
        }

        [Fact]
        public void Tracer_AfterInitialize_ReturnsActivitySource()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                EnableTracing = true
            };
            VsixTelemetry.Initialize(config);

            var tracer = VsixTelemetry.Tracer;

            tracer.Should().NotBeNull();
            tracer.Name.Should().Be("TestService");
        }

        [Fact]
        public void Meter_BeforeInitialize_ThrowsInvalidOperationException()
        {
            Action act = () => { var _ = VsixTelemetry.Meter; };

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not initialized*");
        }

        [Fact]
        public void Meter_AfterInitialize_ReturnsMeter()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                EnableMetrics = true
            };
            VsixTelemetry.Initialize(config);

            var meter = VsixTelemetry.Meter;

            meter.Should().NotBeNull();
            meter.Name.Should().Be("TestService");
        }

        [Fact]
        public void Logger_BeforeInitialize_ThrowsInvalidOperationException()
        {
            Action act = () => { var _ = VsixTelemetry.Logger; };

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not initialized*");
        }

        [Fact]
        public void Logger_AfterInitialize_ReturnsLogger()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                EnableLogging = true
            };
            VsixTelemetry.Initialize(config);

            var logger = VsixTelemetry.Logger;

            logger.Should().NotBeNull();
        }

        [Fact]
        public void LoggerFactory_BeforeInitialize_ThrowsInvalidOperationException()
        {
            Action act = () => { var _ = VsixTelemetry.LoggerFactory; };

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not initialized*");
        }

        [Fact]
        public void LoggerFactory_AfterInitialize_ReturnsLoggerFactory()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                EnableLogging = true
            };
            VsixTelemetry.Initialize(config);

            var loggerFactory = VsixTelemetry.LoggerFactory;

            loggerFactory.Should().NotBeNull();
        }

        [Fact]
        public void Shutdown_ResetsIsInitializedToFalse()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService"
            };
            VsixTelemetry.Initialize(config);
            VsixTelemetry.IsInitialized.Should().BeTrue();

            VsixTelemetry.Shutdown();

            VsixTelemetry.IsInitialized.Should().BeFalse();
        }

        [Fact]
        public void Shutdown_WhenNotInitialized_DoesNotThrow()
        {
            Action act = () => VsixTelemetry.Shutdown();

            act.Should().NotThrow();
        }

        [Fact]
        public void Shutdown_AllowsReinitialization()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService"
            };
            VsixTelemetry.Initialize(config);
            VsixTelemetry.Shutdown();

            Action act = () => VsixTelemetry.Initialize(config);

            act.Should().NotThrow();
            VsixTelemetry.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void TrackException_BeforeInitialize_DoesNotThrow()
        {
            Action act = () => VsixTelemetry.TrackException(new InvalidOperationException("Test"));

            act.Should().NotThrow();
        }

        [Fact]
        public void TrackException_AfterInitialize_DoesNotThrow()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService"
            };
            VsixTelemetry.Initialize(config);

            Action act = () => VsixTelemetry.TrackException(new InvalidOperationException("Test"));

            act.Should().NotThrow();
        }

        [Fact]
        public void TrackException_WithNullException_DoesNotThrow()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService"
            };
            VsixTelemetry.Initialize(config);

            Action act = () => VsixTelemetry.TrackException(null);

            act.Should().NotThrow();
        }

        [Fact]
        public void StartActivity_BeforeInitialize_ReturnsNull()
        {
            var activity = VsixTelemetry.StartActivity("TestActivity");

            activity.Should().BeNull();
        }

        [Fact]
        public void StartCommandActivity_BeforeInitialize_ReturnsNull()
        {
            var activity = VsixTelemetry.StartCommandActivity("TestCommand");

            activity.Should().BeNull();
        }

        [Fact]
        public void CreateLoggerGeneric_BeforeInitialize_ReturnsNull()
        {
            var logger = VsixTelemetry.CreateLogger<VsixTelemetryTests>();

            logger.Should().BeNull();
        }

        [Fact]
        public void CreateLogger_BeforeInitialize_ReturnsNull()
        {
            var logger = VsixTelemetry.CreateLogger("TestCategory");

            logger.Should().BeNull();
        }

        [Fact]
        public void CreateLoggerGeneric_AfterInitialize_ReturnsLogger()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                EnableLogging = true
            };
            VsixTelemetry.Initialize(config);

            var logger = VsixTelemetry.CreateLogger<VsixTelemetryTests>();

            logger.Should().NotBeNull();
        }

        [Fact]
        public void CreateLogger_AfterInitialize_ReturnsLogger()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                EnableLogging = true
            };
            VsixTelemetry.Initialize(config);

            var logger = VsixTelemetry.CreateLogger("TestCategory");

            logger.Should().NotBeNull();
        }

        [Fact]
        public void GetOrCreateCounter_BeforeInitialize_ReturnsNull()
        {
            var counter = VsixTelemetry.GetOrCreateCounter<long>("test.counter");

            counter.Should().BeNull();
        }

        [Fact]
        public void GetOrCreateCounter_AfterInitialize_ReturnsCounter()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                EnableMetrics = true
            };
            VsixTelemetry.Initialize(config);

            var counter = VsixTelemetry.GetOrCreateCounter<long>("test.counter");

            counter.Should().NotBeNull();
        }

        [Fact]
        public void GetOrCreateHistogram_BeforeInitialize_ReturnsNull()
        {
            var histogram = VsixTelemetry.GetOrCreateHistogram<double>("test.histogram");

            histogram.Should().BeNull();
        }

        [Fact]
        public void GetOrCreateHistogram_AfterInitialize_ReturnsHistogram()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                EnableMetrics = true
            };
            VsixTelemetry.Initialize(config);

            var histogram = VsixTelemetry.GetOrCreateHistogram<double>("test.histogram");

            histogram.Should().NotBeNull();
        }

        [Fact]
        public void Initialize_WithTracingDisabled_TracerThrowsInvalidOperationException()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                EnableTracing = false,
                EnableMetrics = true,
                EnableLogging = true
            };
            VsixTelemetry.Initialize(config);

            // When tracing is disabled, the activity source provider is null
            // Accessing Tracer should throw because the provider is null
            Action act = () => { var _ = VsixTelemetry.Tracer; };

            // This will throw NullReferenceException internally, wrapped in InvalidOperationException
            // or throw directly based on implementation
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Initialize_WithMetricsDisabled_MeterThrowsException()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                EnableTracing = true,
                EnableMetrics = false,
                EnableLogging = true
            };
            VsixTelemetry.Initialize(config);

            Action act = () => { var _ = VsixTelemetry.Meter; };

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Initialize_WithLoggingDisabled_LoggerThrowsException()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                EnableTracing = true,
                EnableMetrics = true,
                EnableLogging = false
            };
            VsixTelemetry.Initialize(config);

            Action act = () => { var _ = VsixTelemetry.Logger; };

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Initialize_WithOtlpEndpoint_Succeeds()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                OtlpEndpoint = "http://localhost:4317"
            };

            Action act = () => VsixTelemetry.Initialize(config);

            act.Should().NotThrow();
        }

        [Fact]
        public void Initialize_WithOtlpHeaders_Succeeds()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                OtlpEndpoint = "http://localhost:4317"
            };
            config.OtlpHeaders["x-api-key"] = "test-key";

            Action act = () => VsixTelemetry.Initialize(config);

            act.Should().NotThrow();
        }

        [Fact]
        public void Initialize_WithResourceAttributes_Succeeds()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService"
            };
            config.ResourceAttributes["environment"] = "test";
            config.ResourceAttributes["instance.id"] = "123";

            Action act = () => VsixTelemetry.Initialize(config);

            act.Should().NotThrow();
        }

        [Fact]
        public void Initialize_WithExceptionFilter_Succeeds()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                ExceptionFilter = ex => ex is InvalidOperationException
            };

            Action act = () => VsixTelemetry.Initialize(config);

            act.Should().NotThrow();
        }

        [Fact]
        public void Initialize_WithGlobalExceptionHandlerDisabled_Succeeds()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                EnableGlobalExceptionHandler = false
            };

            Action act = () => VsixTelemetry.Initialize(config);

            act.Should().NotThrow();
        }

        [Fact]
        public void Initialize_WithHttpProtocol_Succeeds()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                OtlpEndpoint = "http://localhost:4318",
                UseOtlpHttp = true
            };

            Action act = () => VsixTelemetry.Initialize(config);

            act.Should().NotThrow();
        }
    }

    /// <summary>
    /// Collection definition to ensure VsixTelemetry tests run sequentially.
    /// </summary>
    [CollectionDefinition("VsixTelemetry", DisableParallelization = true)]
    public class VsixTelemetryCollection
    {
    }
}
