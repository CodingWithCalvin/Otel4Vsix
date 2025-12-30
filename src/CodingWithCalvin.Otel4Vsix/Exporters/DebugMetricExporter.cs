using System.Text;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace CodingWithCalvin.Otel4Vsix.Exporters;

/// <summary>
/// A metric exporter that writes metric data to <see cref="System.Diagnostics.Debug"/>.
/// Output appears in the Visual Studio Output window when debugging.
/// </summary>
internal sealed class DebugMetricExporter : BaseExporter<Metric>
{
    private readonly string _prefix;

    public DebugMetricExporter(string serviceName, string serviceVersion)
    {
        _prefix = $"[{serviceName} v{serviceVersion}]";
    }

    /// <inheritdoc />
    public override ExportResult Export(in Batch<Metric> batch)
    {
        foreach (var metric in batch)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{_prefix} [METRIC] {metric.Name}");

            if (!string.IsNullOrEmpty(metric.Unit))
            {
                sb.AppendLine($"  Unit: {metric.Unit}");
            }

            if (!string.IsNullOrEmpty(metric.Description))
            {
                sb.AppendLine($"  Description: {metric.Description}");
            }

            foreach (var metricPoint in metric.GetMetricPoints())
            {
                sb.Append("  ");

                switch (metric.MetricType)
                {
                    case MetricType.LongSum:
                    case MetricType.LongSumNonMonotonic:
                        sb.AppendLine($"Value: {metricPoint.GetSumLong()}");
                        break;
                    case MetricType.DoubleSum:
                    case MetricType.DoubleSumNonMonotonic:
                        sb.AppendLine($"Value: {metricPoint.GetSumDouble()}");
                        break;
                    case MetricType.LongGauge:
                        sb.AppendLine($"Value: {metricPoint.GetGaugeLastValueLong()}");
                        break;
                    case MetricType.DoubleGauge:
                        sb.AppendLine($"Value: {metricPoint.GetGaugeLastValueDouble()}");
                        break;
                    case MetricType.Histogram:
                        sb.AppendLine($"Count: {metricPoint.GetHistogramCount()}, Sum: {metricPoint.GetHistogramSum()}");
                        break;
                    default:
                        sb.AppendLine($"Type: {metric.MetricType}");
                        break;
                }

                foreach (var tag in metricPoint.Tags)
                {
                    sb.AppendLine($"    {tag.Key}: {tag.Value}");
                }
            }

            System.Diagnostics.Trace.WriteLine(sb.ToString());
        }

        return ExportResult.Success;
    }
}
