using System;
using System.Diagnostics.Metrics;
using CodingWithCalvin.Otel4Vsix.Metrics;
using FluentAssertions;
using Xunit;

namespace CodingWithCalvin.Otel4Vsix.Tests
{
    public class MetricsProviderTests : IDisposable
    {
        private MetricsProvider _provider;

        public void Dispose()
        {
            _provider?.Dispose();
        }

        [Fact]
        public void Constructor_WithNullServiceName_ThrowsArgumentNullException()
        {
            Action act = () => new MetricsProvider(null, "1.0.0");

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("serviceName");
        }

        [Fact]
        public void Constructor_WithEmptyServiceName_ThrowsArgumentNullException()
        {
            Action act = () => new MetricsProvider(string.Empty, "1.0.0");

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithWhitespaceServiceName_ThrowsArgumentNullException()
        {
            Action act = () => new MetricsProvider("   ", "1.0.0");

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithValidName_CreatesMeter()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");

            _provider.Meter.Should().NotBeNull();
            _provider.Meter.Name.Should().Be("TestService");
            _provider.Meter.Version.Should().Be("1.0.0");
        }

        [Fact]
        public void Constructor_WithNullVersion_UsesDefaultVersion()
        {
            _provider = new MetricsProvider("TestService", null);

            _provider.Meter.Should().NotBeNull();
            _provider.Meter.Version.Should().Be("1.0.0");
        }

        [Fact]
        public void Meter_BeforeDispose_ReturnsMeter()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");

            var meter = _provider.Meter;

            meter.Should().NotBeNull();
        }

        [Fact]
        public void Meter_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");
            _provider.Dispose();

            Action act = () => { var _ = _provider.Meter; };

            act.Should().Throw<ObjectDisposedException>()
                .And.ObjectName.Should().Be(nameof(MetricsProvider));
        }

        [Fact]
        public void GetOrCreateCounter_ReturnsCounter()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");

            var counter = _provider.GetOrCreateCounter<long>("test.counter", "items", "A test counter");

            counter.Should().NotBeNull();
        }

        [Fact]
        public void GetOrCreateCounter_ReturnsCachedInstance()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");

            var counter1 = _provider.GetOrCreateCounter<long>("test.counter");
            var counter2 = _provider.GetOrCreateCounter<long>("test.counter");

            counter1.Should().BeSameAs(counter2);
        }

        [Fact]
        public void GetOrCreateCounter_DifferentNames_ReturnsDifferentInstances()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");

            var counter1 = _provider.GetOrCreateCounter<long>("counter.one");
            var counter2 = _provider.GetOrCreateCounter<long>("counter.two");

            counter1.Should().NotBeSameAs(counter2);
        }

        [Fact]
        public void GetOrCreateCounter_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");
            _provider.Dispose();

            Action act = () => _provider.GetOrCreateCounter<long>("test.counter");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void GetOrCreateHistogram_ReturnsHistogram()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");

            var histogram = _provider.GetOrCreateHistogram<double>("test.histogram", "ms", "A test histogram");

            histogram.Should().NotBeNull();
        }

        [Fact]
        public void GetOrCreateHistogram_ReturnsCachedInstance()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");

            var histogram1 = _provider.GetOrCreateHistogram<double>("test.histogram");
            var histogram2 = _provider.GetOrCreateHistogram<double>("test.histogram");

            histogram1.Should().BeSameAs(histogram2);
        }

        [Fact]
        public void GetOrCreateHistogram_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");
            _provider.Dispose();

            Action act = () => _provider.GetOrCreateHistogram<double>("test.histogram");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void GetOrCreateUpDownCounter_ReturnsUpDownCounter()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");

            var counter = _provider.GetOrCreateUpDownCounter<long>("test.updown", "items", "A test up-down counter");

            counter.Should().NotBeNull();
        }

        [Fact]
        public void GetOrCreateUpDownCounter_ReturnsCachedInstance()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");

            var counter1 = _provider.GetOrCreateUpDownCounter<long>("test.updown");
            var counter2 = _provider.GetOrCreateUpDownCounter<long>("test.updown");

            counter1.Should().BeSameAs(counter2);
        }

        [Fact]
        public void GetOrCreateUpDownCounter_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");
            _provider.Dispose();

            Action act = () => _provider.GetOrCreateUpDownCounter<long>("test.updown");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void CreateCommandExecutionCounter_ReturnsCounterWithCorrectName()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");

            var counter = _provider.CreateCommandExecutionCounter();

            counter.Should().NotBeNull();
            // The counter is created, we can verify it's cached
            var counter2 = _provider.GetOrCreateCounter<long>("vs.extension.commands.executed");
            counter.Should().BeSameAs(counter2);
        }

        [Fact]
        public void CreateOperationDurationHistogram_ReturnsHistogramWithCorrectName()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");

            var histogram = _provider.CreateOperationDurationHistogram();

            histogram.Should().NotBeNull();
            // The histogram is created, we can verify it's cached
            var histogram2 = _provider.GetOrCreateHistogram<double>("vs.extension.operation.duration");
            histogram.Should().BeSameAs(histogram2);
        }

        [Fact]
        public void CreateErrorCounter_ReturnsCounterWithCorrectName()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");

            var counter = _provider.CreateErrorCounter();

            counter.Should().NotBeNull();
            // The counter is created, we can verify it's cached
            var counter2 = _provider.GetOrCreateCounter<long>("vs.extension.errors");
            counter.Should().BeSameAs(counter2);
        }

        [Fact]
        public void Dispose_MultipleTimes_DoesNotThrow()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");

            Action act = () =>
            {
                _provider.Dispose();
                _provider.Dispose();
                _provider.Dispose();
            };

            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_ClearsInstrumentCache()
        {
            _provider = new MetricsProvider("TestService", "1.0.0");
            _provider.GetOrCreateCounter<long>("test.counter");

            _provider.Dispose();

            // After dispose, accessing anything should throw
            Action act = () => _provider.GetOrCreateCounter<long>("test.counter");
            act.Should().Throw<ObjectDisposedException>();
        }
    }
}
