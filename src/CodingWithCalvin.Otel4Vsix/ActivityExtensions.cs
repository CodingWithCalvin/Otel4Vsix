using System;
using System.Diagnostics;
using OtelTrace = OpenTelemetry.Trace;

namespace CodingWithCalvin.Otel4Vsix;

/// <summary>
/// Extension methods for <see cref="Activity"/> that wrap OpenTelemetry functionality.
/// These extensions allow consumers to use telemetry features without directly
/// referencing OpenTelemetry namespaces.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Records an exception on the activity.
    /// </summary>
    /// <param name="activity">The activity to record the exception on.</param>
    /// <param name="exception">The exception to record.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity RecordException(this Activity activity, Exception exception)
    {
        if (activity == null || exception == null)
        {
            return activity;
        }

        // Use the OpenTelemetry extension method via explicit call
        activity.AddException(exception);
        return activity;
    }

    /// <summary>
    /// Sets the activity status to error with the specified message.
    /// </summary>
    /// <param name="activity">The activity to set the status on.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity SetErrorStatus(this Activity activity, string errorMessage)
    {
        if (activity == null)
        {
            return activity;
        }

        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
        return activity;
    }

    /// <summary>
    /// Sets the activity status to OK.
    /// </summary>
    /// <param name="activity">The activity to set the status on.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity SetOkStatus(this Activity activity)
    {
        if (activity == null)
        {
            return activity;
        }

        activity.SetStatus(ActivityStatusCode.Ok);
        return activity;
    }

    /// <summary>
    /// Records an exception and sets the activity status to error.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="exception">The exception to record.</param>
    /// <returns>The activity for method chaining.</returns>
    public static Activity RecordError(this Activity activity, Exception exception)
    {
        if (activity == null || exception == null)
        {
            return activity;
        }

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.RecordException(exception);
        return activity;
    }
}
