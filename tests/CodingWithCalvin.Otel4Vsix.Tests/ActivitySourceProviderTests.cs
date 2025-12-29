using System;
using System.Diagnostics;
using CodingWithCalvin.Otel4Vsix.Tracing;
using FluentAssertions;
using Xunit;

namespace CodingWithCalvin.Otel4Vsix.Tests
{
    public class ActivitySourceProviderTests : IDisposable
    {
        private ActivitySourceProvider _provider;
        private ActivityListener _listener;

        public ActivitySourceProviderTests()
        {
            // Set up an activity listener so activities are actually created
            _listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            _provider?.Dispose();
            _listener?.Dispose();
        }

        [Fact]
        public void Constructor_WithNullServiceName_ThrowsArgumentNullException()
        {
            Action act = () => new ActivitySourceProvider(null, "1.0.0");

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("serviceName");
        }

        [Fact]
        public void Constructor_WithEmptyServiceName_ThrowsArgumentNullException()
        {
            Action act = () => new ActivitySourceProvider(string.Empty, "1.0.0");

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithWhitespaceServiceName_ThrowsArgumentNullException()
        {
            Action act = () => new ActivitySourceProvider("   ", "1.0.0");

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithValidName_CreatesActivitySource()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");

            _provider.ActivitySource.Should().NotBeNull();
            _provider.ActivitySource.Name.Should().Be("TestService");
            _provider.ActivitySource.Version.Should().Be("1.0.0");
        }

        [Fact]
        public void Constructor_WithNullVersion_UsesDefaultVersion()
        {
            _provider = new ActivitySourceProvider("TestService", null);

            _provider.ActivitySource.Should().NotBeNull();
            _provider.ActivitySource.Version.Should().Be("1.0.0");
        }

        [Fact]
        public void ActivitySource_BeforeDispose_ReturnsActivitySource()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");

            var activitySource = _provider.ActivitySource;

            activitySource.Should().NotBeNull();
        }

        [Fact]
        public void ActivitySource_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");
            _provider.Dispose();

            Action act = () => { var _ = _provider.ActivitySource; };

            act.Should().Throw<ObjectDisposedException>()
                .And.ObjectName.Should().Be(nameof(ActivitySourceProvider));
        }

        [Fact]
        public void StartActivity_CreatesActivity()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");

            using var activity = _provider.StartActivity("TestActivity");

            activity.Should().NotBeNull();
            activity.OperationName.Should().Be("TestActivity");
        }

        [Fact]
        public void StartActivity_WithKind_SetsActivityKind()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");

            using var activity = _provider.StartActivity("TestActivity", ActivityKind.Client);

            activity.Should().NotBeNull();
            activity.Kind.Should().Be(ActivityKind.Client);
        }

        [Fact]
        public void StartActivity_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");
            _provider.Dispose();

            Action act = () => _provider.StartActivity("TestActivity");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void StartCommandActivity_CreatesActivityWithCorrectName()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");

            using var activity = _provider.StartCommandActivity("MyCommand");

            activity.Should().NotBeNull();
            activity.OperationName.Should().Be("Command: MyCommand");
        }

        [Fact]
        public void StartCommandActivity_SetsCommandNameTag()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");

            using var activity = _provider.StartCommandActivity("MyCommand");

            activity.Should().NotBeNull();
            activity.GetTagItem("vs.command.name").Should().Be("MyCommand");
        }

        [Fact]
        public void StartCommandActivity_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");
            _provider.Dispose();

            Action act = () => _provider.StartCommandActivity("MyCommand");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void StartToolWindowActivity_CreatesActivityWithCorrectName()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");

            using var activity = _provider.StartToolWindowActivity("SolutionExplorer", "Open");

            activity.Should().NotBeNull();
            activity.OperationName.Should().Be("ToolWindow: SolutionExplorer.Open");
        }

        [Fact]
        public void StartToolWindowActivity_SetsCorrectTags()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");

            using var activity = _provider.StartToolWindowActivity("SolutionExplorer", "Open");

            activity.Should().NotBeNull();
            activity.GetTagItem("vs.toolwindow.name").Should().Be("SolutionExplorer");
            activity.GetTagItem("vs.toolwindow.operation").Should().Be("Open");
        }

        [Fact]
        public void StartToolWindowActivity_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");
            _provider.Dispose();

            Action act = () => _provider.StartToolWindowActivity("Window", "Op");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void StartDocumentActivity_CreatesActivityWithCorrectName()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");

            using var activity = _provider.StartDocumentActivity("C:\\file.cs", "Open");

            activity.Should().NotBeNull();
            activity.OperationName.Should().Be("Document: Open");
        }

        [Fact]
        public void StartDocumentActivity_SetsCorrectTags()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");

            using var activity = _provider.StartDocumentActivity("C:\\file.cs", "Save");

            activity.Should().NotBeNull();
            activity.GetTagItem("vs.document.path").Should().Be("C:\\file.cs");
            activity.GetTagItem("vs.document.operation").Should().Be("Save");
        }

        [Fact]
        public void StartDocumentActivity_AfterDispose_ThrowsObjectDisposedException()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");
            _provider.Dispose();

            Action act = () => _provider.StartDocumentActivity("file.cs", "Open");

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_MultipleTimes_DoesNotThrow()
        {
            _provider = new ActivitySourceProvider("TestService", "1.0.0");

            Action act = () =>
            {
                _provider.Dispose();
                _provider.Dispose();
                _provider.Dispose();
            };

            act.Should().NotThrow();
        }
    }
}
