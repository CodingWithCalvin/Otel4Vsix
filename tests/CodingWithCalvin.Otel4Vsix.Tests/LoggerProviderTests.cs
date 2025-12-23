using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Otel4Vsix.Logging;
using Xunit;

namespace CodingWithCalvin.Otel4Vsix.Tests
{
    public class LoggerProviderTests : IDisposable
    {
        private LoggerProvider _provider;
        private Mock<ILoggerFactory> _mockLoggerFactory;
        private Mock<ILogger> _mockLogger;

        public LoggerProviderTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockLoggerFactory
                .Setup(f => f.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);
        }

        public void Dispose()
        {
            _provider?.Dispose();
        }

        [Fact]
        public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
        {
            Action act = () => new LoggerProvider(null, "TestCategory");

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("loggerFactory");
        }

        [Fact]
        public void Constructor_WithValidLoggerFactory_Succeeds()
        {
            Action act = () => _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            act.Should().NotThrow();
        }

        [Fact]
        public void Constructor_WithNullCategoryName_UsesDefaultCategory()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, null);

            _mockLoggerFactory.Verify(f => f.CreateLogger("Otel4Vsix"), Times.Once);
        }

        [Fact]
        public void Constructor_WithCategoryName_UsesProvidedCategory()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "CustomCategory");

            _mockLoggerFactory.Verify(f => f.CreateLogger("CustomCategory"), Times.Once);
        }

        [Fact]
        public void Logger_BeforeDispose_ReturnsLogger()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            var logger = _provider.Logger;

            logger.Should().NotBeNull();
            logger.Should().BeSameAs(_mockLogger.Object);
        }

        [Fact]
        public void Logger_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");
            _provider.Dispose();

            Action act = () => { var _ = _provider.Logger; };

            act.Should().Throw<ObjectDisposedException>()
                .And.ObjectName.Should().Be(nameof(LoggerProvider));
        }

        [Fact]
        public void LoggerFactory_BeforeDispose_ReturnsFactory()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            var factory = _provider.LoggerFactory;

            factory.Should().NotBeNull();
            factory.Should().BeSameAs(_mockLoggerFactory.Object);
        }

        [Fact]
        public void LoggerFactory_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");
            _provider.Dispose();

            Action act = () => { var _ = _provider.LoggerFactory; };

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void CreateLogger_DelegatesToFactory()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            _provider.CreateLogger("NewCategory");

            _mockLoggerFactory.Verify(f => f.CreateLogger("NewCategory"), Times.Once);
        }

        [Fact]
        public void CreateLogger_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");
            _provider.Dispose();

            Action act = () => _provider.CreateLogger("NewCategory");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void CreateLoggerGeneric_DelegatesToFactory()
        {
            var mockGenericLogger = new Mock<ILogger<LoggerProviderTests>>();
            _mockLoggerFactory
                .Setup(f => f.CreateLogger(typeof(LoggerProviderTests).FullName))
                .Returns(mockGenericLogger.Object);

            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            var logger = _provider.CreateLogger<LoggerProviderTests>();

            logger.Should().NotBeNull();
        }

        [Fact]
        public void CreateLoggerGeneric_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");
            _provider.Dispose();

            Action act = () => _provider.CreateLogger<LoggerProviderTests>();

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void LogInformation_DelegatesToUnderlyingLogger()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            _provider.LogInformation("Test message {Param}", "value");

            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogInformation_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");
            _provider.Dispose();

            Action act = () => _provider.LogInformation("Test");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void LogWarning_DelegatesToUnderlyingLogger()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            _provider.LogWarning("Warning message");

            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogWarning_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");
            _provider.Dispose();

            Action act = () => _provider.LogWarning("Test");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void LogError_DelegatesToUnderlyingLogger()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            _provider.LogError("Error message");

            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogError_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");
            _provider.Dispose();

            Action act = () => _provider.LogError("Test");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void LogErrorWithException_DelegatesToUnderlyingLogger()
        {
            var exception = new InvalidOperationException("Test exception");
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            _provider.LogError(exception, "Error with exception");

            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogErrorWithException_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");
            _provider.Dispose();

            Action act = () => _provider.LogError(new Exception(), "Test");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void LogDebug_DelegatesToUnderlyingLogger()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            _provider.LogDebug("Debug message");

            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogDebug_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");
            _provider.Dispose();

            Action act = () => _provider.LogDebug("Test");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void LogCritical_DelegatesToUnderlyingLogger()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            _provider.LogCritical("Critical message");

            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Critical,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogCritical_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");
            _provider.Dispose();

            Action act = () => _provider.LogCritical("Test");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void LogCriticalWithException_DelegatesToUnderlyingLogger()
        {
            var exception = new InvalidOperationException("Critical exception");
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            _provider.LogCritical(exception, "Critical with exception");

            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Critical,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogCriticalWithException_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");
            _provider.Dispose();

            Action act = () => _provider.LogCritical(new Exception(), "Test");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_DisposesUnderlyingFactory()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            _provider.Dispose();

            _mockLoggerFactory.Verify(f => f.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_MultipleTimes_DoesNotThrow()
        {
            _provider = new LoggerProvider(_mockLoggerFactory.Object, "TestCategory");

            Action act = () =>
            {
                _provider.Dispose();
                _provider.Dispose();
                _provider.Dispose();
            };

            act.Should().NotThrow();
            _mockLoggerFactory.Verify(f => f.Dispose(), Times.Once);
        }
    }
}
