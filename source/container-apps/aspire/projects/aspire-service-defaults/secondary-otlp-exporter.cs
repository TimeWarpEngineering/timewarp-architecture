#region Purpose
// Registers a second OTLP exporter when Observability:SecondaryOtlp:Endpoint is set, beside the Aspire dashboard exporter.
#endregion

#region Design
// Task 536-001. OpenTelemetry.Exporter.OpenTelemetryProtocol 1.21 throws NotSupportedException from
// AddOtlpExporter when UseOtlpExporter has already registered the cross-cutting exporter. While a
// secondary endpoint is configured this type registers the primary exporter (when
// OTEL_EXPORTER_OTLP_ENDPOINT is set) and one named exporter per signal (secondary-traces,
// secondary-metrics, secondary-logs). The caller skips UseOtlpExporter. An unset, blank, or
// non-http(s) endpoint registers nothing and returns false so the caller keeps UseOtlpExporter.
// Each name has its own OtlpExporterOptions. Setting Endpoint clears AppendSignalPathToEndpoint,
// which is not a public setter. An http/protobuf exporter is therefore given the full signal URL
// (base plus v1/traces, v1/metrics, or v1/logs). gRPC is given the base URI; its export client
// appends the service path either way. Headers are optional. When they are unset the callback sets
// Headers to empty so the dashboard API key Aspire puts in OTEL_EXPORTER_OTLP_HEADERS is not sent
// to the second collector. Header values are not logged. Protocol is grpc or http/protobuf; any
// other text leaves the protocol the process environment already selected. Export failures,
// including an unreachable collector, stay inside the batch exporter and do not fail host startup
// or request handling.
#endregion

namespace Microsoft.Extensions.Hosting;

using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;

/// <summary>Secondary OTLP export beside <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> (task 536-001).</summary>
public static class SecondaryOtlpExporterExtensions
{
  private const string EndpointKey = "Observability:SecondaryOtlp:Endpoint";
  private const string HeadersKey = "Observability:SecondaryOtlp:Headers";
  private const string ProtocolKey = "Observability:SecondaryOtlp:Protocol";
  private const string PrimaryEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";
  private const string TracesExporterName = "secondary-traces";
  private const string MetricsExporterName = "secondary-metrics";
  private const string LogsExporterName = "secondary-logs";
  private const string TracesSignalPath = "v1/traces";
  private const string MetricsSignalPath = "v1/metrics";
  private const string LogsSignalPath = "v1/logs";

  /// <summary>
  /// Registers one named secondary OTLP exporter per signal when
  /// <c>Observability:SecondaryOtlp:Endpoint</c> is an absolute http or https URL.
  /// </summary>
  /// <returns>
  /// <see langword="true"/> when the secondary exporter is registered. The caller must not also call
  /// <c>UseOtlpExporter</c>; OpenTelemetry 1.21 throws when both are registered.
  /// </returns>
  public static bool TryAddSecondaryOtlpExporters(this IHostApplicationBuilder builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    string? configured = builder.Configuration[EndpointKey];
    if (string.IsNullOrWhiteSpace(configured) || !IsAbsoluteHttp(configured.Trim()))
    {
      return false;
    }

    Uri endpoint = new(configured.Trim());
    string? headers = BlankToNull(builder.Configuration[HeadersKey]);
    OtlpExportProtocol? protocol = TryParseProtocol(builder.Configuration[ProtocolKey]);
    bool exportPrimary = !string.IsNullOrWhiteSpace(builder.Configuration[PrimaryEndpointKey]);

    void ConfigureSecondary(OtlpExporterOptions options, string signalPath)
    {
      if (protocol is not null)
      {
        options.Protocol = protocol.Value;
      }

      // The Endpoint setter clears signal-path appending. Http gets the full signal URL.
      // gRPC gets the base URI and appends its service path itself.
      options.Endpoint = options.Protocol == OtlpExportProtocol.HttpProtobuf
        ? WithSignalPath(endpoint, signalPath)
        : endpoint;

      // Empty when unset so the dashboard API key inherited from the process environment is not sent.
      options.Headers = headers ?? "";
    }

    builder.Services.AddOpenTelemetry()
      .WithTracing(tracing =>
      {
        if (exportPrimary)
        {
          tracing.AddOtlpExporter();
        }

        tracing.AddOtlpExporter(TracesExporterName, options => ConfigureSecondary(options, TracesSignalPath));
      })
      .WithMetrics(metrics =>
      {
        if (exportPrimary)
        {
          metrics.AddOtlpExporter();
        }

        metrics.AddOtlpExporter(MetricsExporterName, options => ConfigureSecondary(options, MetricsSignalPath));
      });

    builder.Services.ConfigureOpenTelemetryLoggerProvider(logging =>
    {
      if (exportPrimary)
      {
        logging.AddOtlpExporter();
      }

      logging.AddOtlpExporter(LogsExporterName, options => ConfigureSecondary(options, LogsSignalPath));
    });

    return true;
  }

  private static Uri WithSignalPath(Uri endpoint, string signalPath)
  {
    string absolute = endpoint.AbsoluteUri;
    if (absolute.EndsWith(signalPath, StringComparison.OrdinalIgnoreCase))
    {
      return endpoint;
    }

    string separator = absolute.Length > 0 && absolute[^1] == '/' ? "" : "/";
    return new Uri(absolute + separator + signalPath);
  }

  private static bool IsAbsoluteHttp(string candidate) =>
    Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri)
      && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
      && !string.IsNullOrEmpty(uri.Host);

  private static OtlpExportProtocol? TryParseProtocol(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    string candidate = value.Trim();
    if (candidate.Equals("grpc", StringComparison.OrdinalIgnoreCase))
    {
      return OtlpExportProtocol.Grpc;
    }

    if (candidate.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase))
    {
      return OtlpExportProtocol.HttpProtobuf;
    }

    return null;
  }

  private static string? BlankToNull(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
