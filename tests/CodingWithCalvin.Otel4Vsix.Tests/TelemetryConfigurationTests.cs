using System;
using FluentAssertions;
using Otel4Vsix;
using Xunit;

namespace CodingWithCalvin.Otel4Vsix.Tests
{
    public class TelemetryConfigurationTests
    {
        [Fact]
        public void Defaults_AreSetCorrectly()
        {
            var config = new TelemetryConfiguration();

            config.ServiceName.Should().Be("VsixExtension");
            config.ServiceVersion.Should().Be("1.0.0");
            config.OtlpEndpoint.Should().BeNull();
            config.UseOtlpHttp.Should().BeFalse();
            config.EnableConsoleExporter.Should().BeFalse();
            config.EnableTracing.Should().BeTrue();
            config.EnableMetrics.Should().BeTrue();
            config.EnableLogging.Should().BeTrue();
            config.EnableGlobalExceptionHandler.Should().BeTrue();
            config.TraceSamplingRatio.Should().Be(1.0);
            config.ExceptionFilter.Should().BeNull();
            config.IncludeVisualStudioContext.Should().BeTrue();
            config.ExportTimeoutMilliseconds.Should().Be(30000);
            config.BatchExportScheduledDelayMilliseconds.Should().Be(5000);
        }

        [Fact]
        public void OtlpHeaders_InitializedAsEmptyDictionary()
        {
            var config = new TelemetryConfiguration();

            config.OtlpHeaders.Should().NotBeNull();
            config.OtlpHeaders.Should().BeEmpty();
        }

        [Fact]
        public void ResourceAttributes_InitializedAsEmptyDictionary()
        {
            var config = new TelemetryConfiguration();

            config.ResourceAttributes.Should().NotBeNull();
            config.ResourceAttributes.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WithValidConfig_Succeeds()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "TestService",
                ServiceVersion = "2.0.0",
                TraceSamplingRatio = 0.5,
                ExportTimeoutMilliseconds = 10000,
                BatchExportScheduledDelayMilliseconds = 1000
            };

            var exception = Record.Exception(() => config.Validate());

            exception.Should().BeNull();
        }

        [Fact]
        public void Validate_WithNullServiceName_ThrowsArgumentException()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = null
            };

            var exception = Record.Exception(() => config.Validate());

            exception.Should().BeOfType<ArgumentException>();
            exception.Message.Should().Contain("ServiceName");
        }

        [Fact]
        public void Validate_WithEmptyServiceName_ThrowsArgumentException()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = string.Empty
            };

            var exception = Record.Exception(() => config.Validate());

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void Validate_WithWhitespaceServiceName_ThrowsArgumentException()
        {
            var config = new TelemetryConfiguration
            {
                ServiceName = "   "
            };

            var exception = Record.Exception(() => config.Validate());

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void Validate_WithNegativeSamplingRatio_ThrowsArgumentOutOfRangeException()
        {
            var config = new TelemetryConfiguration
            {
                TraceSamplingRatio = -0.1
            };

            var exception = Record.Exception(() => config.Validate());

            exception.Should().BeOfType<ArgumentOutOfRangeException>();
            ((ArgumentOutOfRangeException)exception).ParamName.Should().Be("TraceSamplingRatio");
        }

        [Fact]
        public void Validate_WithSamplingRatioAboveOne_ThrowsArgumentOutOfRangeException()
        {
            var config = new TelemetryConfiguration
            {
                TraceSamplingRatio = 1.1
            };

            var exception = Record.Exception(() => config.Validate());

            exception.Should().BeOfType<ArgumentOutOfRangeException>();
            ((ArgumentOutOfRangeException)exception).ParamName.Should().Be("TraceSamplingRatio");
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        public void Validate_WithValidSamplingRatio_Succeeds(double samplingRatio)
        {
            var config = new TelemetryConfiguration
            {
                TraceSamplingRatio = samplingRatio
            };

            var exception = Record.Exception(() => config.Validate());

            exception.Should().BeNull();
        }

        [Fact]
        public void Validate_WithZeroExportTimeout_ThrowsArgumentOutOfRangeException()
        {
            var config = new TelemetryConfiguration
            {
                ExportTimeoutMilliseconds = 0
            };

            var exception = Record.Exception(() => config.Validate());

            exception.Should().BeOfType<ArgumentOutOfRangeException>();
            ((ArgumentOutOfRangeException)exception).ParamName.Should().Be("ExportTimeoutMilliseconds");
        }

        [Fact]
        public void Validate_WithNegativeExportTimeout_ThrowsArgumentOutOfRangeException()
        {
            var config = new TelemetryConfiguration
            {
                ExportTimeoutMilliseconds = -1
            };

            var exception = Record.Exception(() => config.Validate());

            exception.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Validate_WithZeroBatchDelay_ThrowsArgumentOutOfRangeException()
        {
            var config = new TelemetryConfiguration
            {
                BatchExportScheduledDelayMilliseconds = 0
            };

            var exception = Record.Exception(() => config.Validate());

            exception.Should().BeOfType<ArgumentOutOfRangeException>();
            ((ArgumentOutOfRangeException)exception).ParamName.Should().Be("BatchExportScheduledDelayMilliseconds");
        }

        [Fact]
        public void Validate_WithNegativeBatchDelay_ThrowsArgumentOutOfRangeException()
        {
            var config = new TelemetryConfiguration
            {
                BatchExportScheduledDelayMilliseconds = -100
            };

            var exception = Record.Exception(() => config.Validate());

            exception.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void OtlpHeaders_CanAddHeaders()
        {
            var config = new TelemetryConfiguration();

            config.OtlpHeaders["x-api-key"] = "test-key";
            config.OtlpHeaders["Authorization"] = "Bearer token";

            config.OtlpHeaders.Should().HaveCount(2);
            config.OtlpHeaders["x-api-key"].Should().Be("test-key");
        }

        [Fact]
        public void ResourceAttributes_CanAddAttributes()
        {
            var config = new TelemetryConfiguration();

            config.ResourceAttributes["environment"] = "test";
            config.ResourceAttributes["instance.id"] = 123;

            config.ResourceAttributes.Should().HaveCount(2);
            config.ResourceAttributes["environment"].Should().Be("test");
        }

        [Fact]
        public void ExceptionFilter_CanBeSet()
        {
            var config = new TelemetryConfiguration();
            Func<Exception, bool> filter = ex => ex is InvalidOperationException;

            config.ExceptionFilter = filter;

            config.ExceptionFilter.Should().NotBeNull();
            config.ExceptionFilter(new InvalidOperationException()).Should().BeTrue();
            config.ExceptionFilter(new ArgumentException()).Should().BeFalse();
        }
    }
}
