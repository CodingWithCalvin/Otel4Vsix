using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Otel4Vsix.Exceptions;
using Xunit;

namespace CodingWithCalvin.Otel4Vsix.Tests
{
    public class ExceptionTrackerTests : IDisposable
    {
        private ExceptionTracker _tracker;
        private Mock<ILogger> _mockLogger;
        private ActivityListener _listener;

        public ExceptionTrackerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _mockLogger.Setup(l => l.BeginScope(It.IsAny<It.IsAnyType>())).Returns(Mock.Of<IDisposable>());

            // Set up an activity listener for activity-related tests
            _listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            _tracker?.Dispose();
            _listener?.Dispose();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Action act = () => new ExceptionTracker(null);

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_WithValidLogger_Succeeds()
        {
            Action act = () => _tracker = new ExceptionTracker(_mockLogger.Object);

            act.Should().NotThrow();
        }

        [Fact]
        public void Constructor_WithExceptionFilter_Succeeds()
        {
            Func<Exception, bool> filter = ex => ex is InvalidOperationException;

            Action act = () => _tracker = new ExceptionTracker(_mockLogger.Object, filter);

            act.Should().NotThrow();
        }

        [Fact]
        public void TrackException_WithNullException_DoesNotThrow()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);

            Action act = () => _tracker.TrackException(null);

            act.Should().NotThrow();
        }

        [Fact]
        public void TrackException_WithNullException_DoesNotLog()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);

            _tracker.TrackException(null);

            _mockLogger.Verify(
                l => l.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public void TrackException_WithValidException_LogsException()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);
            var exception = new InvalidOperationException("Test exception");

            _tracker.TrackException(exception);

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
        public void TrackException_WithFilterReturningTrue_LogsException()
        {
            Func<Exception, bool> filter = ex => true;
            _tracker = new ExceptionTracker(_mockLogger.Object, filter);
            var exception = new InvalidOperationException("Test exception");

            _tracker.TrackException(exception);

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
        public void TrackException_WithFilterReturningFalse_DoesNotLog()
        {
            Func<Exception, bool> filter = ex => false;
            _tracker = new ExceptionTracker(_mockLogger.Object, filter);
            var exception = new InvalidOperationException("Test exception");

            _tracker.TrackException(exception);

            _mockLogger.Verify(
                l => l.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public void TrackException_WithSelectiveFilter_FiltersCorrectly()
        {
            Func<Exception, bool> filter = ex => ex is InvalidOperationException;
            _tracker = new ExceptionTracker(_mockLogger.Object, filter);
            var allowedException = new InvalidOperationException("Allowed");
            var filteredOutException = new ArgumentException("Filtered");

            _tracker.TrackException(allowedException);
            _tracker.TrackException(filteredOutException);

            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    allowedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    filteredOutException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public void TrackException_BeginsScope()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);
            var exception = new InvalidOperationException("Test exception");

            _tracker.TrackException(exception);

            _mockLogger.Verify(l => l.BeginScope(It.IsAny<It.IsAnyType>()), Times.Once);
        }

        [Fact]
        public void TrackException_WithAdditionalAttributes_IncludesAttributes()
        {
            Dictionary<string, object> capturedScope = null;
            _mockLogger.Setup(l => l.BeginScope(It.IsAny<Dictionary<string, object>>()))
                .Callback<Dictionary<string, object>>(scope => capturedScope = scope)
                .Returns(Mock.Of<IDisposable>());

            _tracker = new ExceptionTracker(_mockLogger.Object);
            var exception = new InvalidOperationException("Test exception");
            var additionalAttributes = new Dictionary<string, object>
            {
                { "custom.key", "custom.value" },
                { "custom.number", 42 }
            };

            _tracker.TrackException(exception, additionalAttributes);

            capturedScope.Should().NotBeNull();
            capturedScope.Should().ContainKey("custom.key");
            capturedScope["custom.key"].Should().Be("custom.value");
            capturedScope.Should().ContainKey("custom.number");
            capturedScope["custom.number"].Should().Be(42);
        }

        [Fact]
        public void TrackException_IncludesExceptionType()
        {
            Dictionary<string, object> capturedScope = null;
            _mockLogger.Setup(l => l.BeginScope(It.IsAny<Dictionary<string, object>>()))
                .Callback<Dictionary<string, object>>(scope => capturedScope = scope)
                .Returns(Mock.Of<IDisposable>());

            _tracker = new ExceptionTracker(_mockLogger.Object);
            var exception = new InvalidOperationException("Test");

            _tracker.TrackException(exception);

            capturedScope.Should().ContainKey("exception.type");
            capturedScope["exception.type"].Should().Be(typeof(InvalidOperationException).FullName);
        }

        [Fact]
        public void TrackException_IncludesExceptionMessage()
        {
            Dictionary<string, object> capturedScope = null;
            _mockLogger.Setup(l => l.BeginScope(It.IsAny<Dictionary<string, object>>()))
                .Callback<Dictionary<string, object>>(scope => capturedScope = scope)
                .Returns(Mock.Of<IDisposable>());

            _tracker = new ExceptionTracker(_mockLogger.Object);
            var exception = new InvalidOperationException("Test message");

            _tracker.TrackException(exception);

            capturedScope.Should().ContainKey("exception.message");
            capturedScope["exception.message"].Should().Be("Test message");
        }

        [Fact]
        public void TrackException_IncludesStackTrace()
        {
            Dictionary<string, object> capturedScope = null;
            _mockLogger.Setup(l => l.BeginScope(It.IsAny<Dictionary<string, object>>()))
                .Callback<Dictionary<string, object>>(scope => capturedScope = scope)
                .Returns(Mock.Of<IDisposable>());

            _tracker = new ExceptionTracker(_mockLogger.Object);
            Exception exception;
            try
            {
                throw new InvalidOperationException("Test");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            _tracker.TrackException(exception);

            capturedScope.Should().ContainKey("exception.stacktrace");
            capturedScope["exception.stacktrace"].Should().NotBeNull();
            ((string)capturedScope["exception.stacktrace"]).Should().Contain("TrackException_IncludesStackTrace");
        }

        [Fact]
        public void TrackException_WithInnerException_IncludesInnerExceptionInfo()
        {
            Dictionary<string, object> capturedScope = null;
            _mockLogger.Setup(l => l.BeginScope(It.IsAny<Dictionary<string, object>>()))
                .Callback<Dictionary<string, object>>(scope => capturedScope = scope)
                .Returns(Mock.Of<IDisposable>());

            _tracker = new ExceptionTracker(_mockLogger.Object);
            var innerException = new ArgumentException("Inner message");
            var exception = new InvalidOperationException("Outer message", innerException);

            _tracker.TrackException(exception);

            capturedScope.Should().ContainKey("exception.inner.type");
            capturedScope["exception.inner.type"].Should().Be(typeof(ArgumentException).FullName);
            capturedScope.Should().ContainKey("exception.inner.message");
            capturedScope["exception.inner.message"].Should().Be("Inner message");
        }

        [Fact]
        public void TrackException_WithExceptionData_IncludesDataEntries()
        {
            Dictionary<string, object> capturedScope = null;
            _mockLogger.Setup(l => l.BeginScope(It.IsAny<Dictionary<string, object>>()))
                .Callback<Dictionary<string, object>>(scope => capturedScope = scope)
                .Returns(Mock.Of<IDisposable>());

            _tracker = new ExceptionTracker(_mockLogger.Object);
            var exception = new InvalidOperationException("Test");
            exception.Data["userId"] = "user123";
            exception.Data["operation"] = "save";

            _tracker.TrackException(exception);

            capturedScope.Should().ContainKey("exception.data.userId");
            capturedScope["exception.data.userId"].Should().Be("user123");
            capturedScope.Should().ContainKey("exception.data.operation");
            capturedScope["exception.data.operation"].Should().Be("save");
        }

        [Fact]
        public void TrackException_WithVsContextEnabled_IncludesVsContext()
        {
            Dictionary<string, object> capturedScope = null;
            _mockLogger.Setup(l => l.BeginScope(It.IsAny<Dictionary<string, object>>()))
                .Callback<Dictionary<string, object>>(scope => capturedScope = scope)
                .Returns(Mock.Of<IDisposable>());

            _tracker = new ExceptionTracker(_mockLogger.Object, includeVsContext: true);
            var exception = new InvalidOperationException("Test");

            _tracker.TrackException(exception);

            capturedScope.Should().ContainKey("vs.context.available");
        }

        [Fact]
        public void TrackException_AfterDispose_ThrowsObjectDisposedException()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);
            _tracker.Dispose();

            Action act = () => _tracker.TrackException(new InvalidOperationException("Test"));

            act.Should().Throw<ObjectDisposedException>()
                .And.ObjectName.Should().Be(nameof(ExceptionTracker));
        }

        [Fact]
        public void RecordExceptionOnActivity_WithNullActivity_DoesNotThrow()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);

            Action act = () => _tracker.RecordExceptionOnActivity(null, new InvalidOperationException("Test"));

            act.Should().NotThrow();
        }

        [Fact]
        public void RecordExceptionOnActivity_WithNullException_DoesNotThrow()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);
            using var activitySource = new ActivitySource("Test");
            using var activity = activitySource.StartActivity("TestActivity");

            Action act = () => _tracker.RecordExceptionOnActivity(activity, null);

            act.Should().NotThrow();
        }

        [Fact]
        public void RecordExceptionOnActivity_SetsActivityStatusToError()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);
            using var activitySource = new ActivitySource("Test");
            using var activity = activitySource.StartActivity("TestActivity");
            var exception = new InvalidOperationException("Test error");

            _tracker.RecordExceptionOnActivity(activity, exception);

            activity.Status.Should().Be(ActivityStatusCode.Error);
            activity.StatusDescription.Should().Be("Test error");
        }

        [Fact]
        public void RecordExceptionOnActivity_AddsExceptionEvent()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);
            using var activitySource = new ActivitySource("Test");
            using var activity = activitySource.StartActivity("TestActivity");
            var exception = new InvalidOperationException("Test error");

            _tracker.RecordExceptionOnActivity(activity, exception);

            activity.Events.Should().ContainSingle(e => e.Name == "exception");
        }

        [Fact]
        public void RecordExceptionOnActivity_WithFilter_RespectsFilter()
        {
            Func<Exception, bool> filter = ex => ex is InvalidOperationException;
            _tracker = new ExceptionTracker(_mockLogger.Object, filter);
            using var activitySource = new ActivitySource("Test");
            using var activity = activitySource.StartActivity("TestActivity");
            var filteredOutException = new ArgumentException("Filtered");

            _tracker.RecordExceptionOnActivity(activity, filteredOutException);

            activity.Status.Should().Be(ActivityStatusCode.Unset);
            activity.Events.Should().BeEmpty();
        }

        [Fact]
        public void RecordExceptionOnActivity_AfterDispose_ThrowsObjectDisposedException()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);
            _tracker.Dispose();
            using var activitySource = new ActivitySource("Test");
            using var activity = activitySource.StartActivity("TestActivity");

            Action act = () => _tracker.RecordExceptionOnActivity(activity, new InvalidOperationException());

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void RegisterGlobalExceptionHandler_IsIdempotent()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);

            Action act = () =>
            {
                _tracker.RegisterGlobalExceptionHandler();
                _tracker.RegisterGlobalExceptionHandler();
                _tracker.RegisterGlobalExceptionHandler();
            };

            act.Should().NotThrow();
        }

        [Fact]
        public void RegisterGlobalExceptionHandler_AfterDispose_ThrowsObjectDisposedException()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);
            _tracker.Dispose();

            Action act = () => _tracker.RegisterGlobalExceptionHandler();

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void UnregisterGlobalExceptionHandler_IsIdempotent()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);
            _tracker.RegisterGlobalExceptionHandler();

            Action act = () =>
            {
                _tracker.UnregisterGlobalExceptionHandler();
                _tracker.UnregisterGlobalExceptionHandler();
                _tracker.UnregisterGlobalExceptionHandler();
            };

            act.Should().NotThrow();
        }

        [Fact]
        public void UnregisterGlobalExceptionHandler_WithoutRegistration_DoesNotThrow()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);

            Action act = () => _tracker.UnregisterGlobalExceptionHandler();

            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_UnregistersGlobalHandler()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);
            _tracker.RegisterGlobalExceptionHandler();

            Action act = () => _tracker.Dispose();

            act.Should().NotThrow();
            // The handler is unregistered, subsequent operations will throw ObjectDisposedException
        }

        [Fact]
        public void Dispose_MultipleTimes_DoesNotThrow()
        {
            _tracker = new ExceptionTracker(_mockLogger.Object);
            _tracker.RegisterGlobalExceptionHandler();

            Action act = () =>
            {
                _tracker.Dispose();
                _tracker.Dispose();
                _tracker.Dispose();
            };

            act.Should().NotThrow();
        }
    }
}
