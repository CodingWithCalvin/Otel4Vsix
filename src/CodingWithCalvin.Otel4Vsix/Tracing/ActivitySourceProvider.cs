using System;
using System.Diagnostics;

namespace Otel4Vsix.Tracing
{
    /// <summary>
    /// Provides and manages the <see cref="ActivitySource"/> for creating traces.
    /// </summary>
    internal sealed class ActivitySourceProvider : IDisposable
    {
        private readonly ActivitySource _activitySource;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivitySourceProvider"/> class.
        /// </summary>
        /// <param name="serviceName">The name of the service for trace identification.</param>
        /// <param name="serviceVersion">The version of the service.</param>
        /// <exception cref="ArgumentNullException">Thrown when serviceName is null.</exception>
        public ActivitySourceProvider(string serviceName, string serviceVersion)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            _activitySource = new ActivitySource(serviceName, serviceVersion ?? "1.0.0");
        }

        /// <summary>
        /// Gets the <see cref="ActivitySource"/> for creating activities (spans).
        /// </summary>
        public ActivitySource ActivitySource
        {
            get
            {
                ThrowIfDisposed();
                return _activitySource;
            }
        }

        /// <summary>
        /// Starts a new activity with the specified name.
        /// </summary>
        /// <param name="name">The name of the activity.</param>
        /// <param name="kind">The kind of activity.</param>
        /// <returns>The started activity, or null if no listeners are registered.</returns>
        public Activity StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        {
            ThrowIfDisposed();
            return _activitySource.StartActivity(name, kind);
        }

        /// <summary>
        /// Starts a new activity for a VS command execution.
        /// </summary>
        /// <param name="commandName">The name of the command being executed.</param>
        /// <returns>The started activity, or null if no listeners are registered.</returns>
        public Activity StartCommandActivity(string commandName)
        {
            ThrowIfDisposed();
            var activity = _activitySource.StartActivity($"Command: {commandName}", ActivityKind.Internal);
            activity?.SetTag("vs.command.name", commandName);
            return activity;
        }

        /// <summary>
        /// Starts a new activity for a tool window operation.
        /// </summary>
        /// <param name="toolWindowName">The name of the tool window.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <returns>The started activity, or null if no listeners are registered.</returns>
        public Activity StartToolWindowActivity(string toolWindowName, string operation)
        {
            ThrowIfDisposed();
            var activity = _activitySource.StartActivity($"ToolWindow: {toolWindowName}.{operation}", ActivityKind.Internal);
            activity?.SetTag("vs.toolwindow.name", toolWindowName);
            activity?.SetTag("vs.toolwindow.operation", operation);
            return activity;
        }

        /// <summary>
        /// Starts a new activity for a document operation.
        /// </summary>
        /// <param name="documentPath">The path of the document.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <returns>The started activity, or null if no listeners are registered.</returns>
        public Activity StartDocumentActivity(string documentPath, string operation)
        {
            ThrowIfDisposed();
            var activity = _activitySource.StartActivity($"Document: {operation}", ActivityKind.Internal);
            activity?.SetTag("vs.document.path", documentPath);
            activity?.SetTag("vs.document.operation", operation);
            return activity;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _activitySource?.Dispose();
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ActivitySourceProvider));
            }
        }
    }
}
