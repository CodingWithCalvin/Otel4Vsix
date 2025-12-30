using System.Diagnostics;
using System.Text;
using OpenTelemetry;

namespace CodingWithCalvin.Otel4Vsix.Exporters;

/// <summary>
/// An activity exporter that writes trace data to <see cref="System.Diagnostics.Debug"/>.
/// Output appears in the Visual Studio Output window when debugging.
/// </summary>
internal sealed class DebugActivityExporter : BaseExporter<Activity>
{
    private readonly string _prefix;

    public DebugActivityExporter(string serviceName, string serviceVersion)
    {
        _prefix = $"[{serviceName} v{serviceVersion}]";
    }

    /// <inheritdoc />
    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{_prefix} [TRACE] {activity.DisplayName}");
            sb.AppendLine($"  TraceId: {activity.TraceId}");
            sb.AppendLine($"  SpanId: {activity.SpanId}");
            sb.AppendLine($"  Duration: {activity.Duration.TotalMilliseconds:F2}ms");
            sb.AppendLine($"  Status: {activity.Status}");

            if (activity.Tags != null)
            {
                foreach (var tag in activity.Tags)
                {
                    sb.AppendLine($"  {tag.Key}: {tag.Value}");
                }
            }

            if (activity.Events != null)
            {
                foreach (var evt in activity.Events)
                {
                    sb.AppendLine($"  Event: {evt.Name} at {evt.Timestamp}");
                }
            }

            System.Diagnostics.Trace.WriteLine(sb.ToString());
        }

        return ExportResult.Success;
    }
}
