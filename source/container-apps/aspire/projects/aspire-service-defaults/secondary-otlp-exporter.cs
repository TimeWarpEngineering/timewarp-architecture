#region Purpose
// Registers a second OTLP exporter when Observability:SecondaryOtlp:Endpoint is set, beside the Aspire dashboard exporter.
#endregion

#region Design
// Task 536-001. OpenTelemetry.Exporter.OpenTelemetryProtocol 1.21 throws NotSupportedException from
// AddOtlpExporter when UseOtlpExporter has already registered the cross-cutting exporter. While a
// secondary endpoint is configured this type registers the primary exporter (when
// OTEL_EXPORTER_OTLP_ENDPOINT is set) and a named "secondary" exporter on tracing, metrics, and
// logging, and the caller skips UseOtlpExporter. An unset, blank, or non-http(s) endpoint registers
// nothing and returns false so the caller keeps UseOtlpExporter.
// The named exporter's configure callback sets Endpoint. That public setter clears
// AppendSignalPathToEndpoint, and the HTTP export client then posts to the base URI and drops
// v1/traces, v1/metrics, and v1/logs. The callback turns the flag back on so each signal appends
// its own path from the one shared options object. gRPC appends its service path either way.
// Headers are optional. When they are unset the callback sets Headers to empty so the
// dashboard API key Aspire puts in OTEL_EXPORTER_OTLP_HEADERS is not sent to the second collector.
// Header values are not logged. Protocol is grpc or http/protobuf; any other text leaves the protocol
// the process environment already selected. Export failures, including an unreachable collector, stay
// inside the batch exporter and do not fail host startup or request handling.
#endregion

namespace Microsoft.Extensions.Hosting;

using System.Reflection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;

/// <summary>Secondary OTLP export beside <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> (task 536-001).</summary>
public static class SecondaryOtlpExporterExtensions
{
  private const string EndpointKey = "Observability:SecondaryOtlp:Endpoint";
  private const string HeadersKey = "Observability:SecondaryOtlp:Headers";
  private const string ProtocolKey = "Observability:SecondaryOtlp:Protocol";
  private const string PrimaryEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";
  private const string ExporterName = "secondary";

  /// <summary>
  /// Registers a named secondary OTLP exporter for traces, metrics, and logs when
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

    void ConfigureSecondary(OtlpExporterOptions options)
    {
      options.Endpoint = endpoint;
      RestoreSignalPathAppending(options);
      if (protocol is not null)
      {
        options.Protocol = protocol.Value;
      }

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

        tracing.AddOtlpExporter(ExporterName, ConfigureSecondary);
      })
      .WithMetrics(metrics =>
      {
        if (exportPrimary)
        {
          metrics.AddOtlpExporter();
        }

        metrics.AddOtlpExporter(ExporterName, ConfigureSecondary);
      });

    builder.Services.ConfigureOpenTelemetryLoggerProvider(logging =>
    {
      if (exportPrimary)
      {
        logging.AddOtlpExporter();
      }

      logging.AddOtlpExporter(ExporterName, ConfigureSecondary);
    });

    return true;
  }

  private static void RestoreSignalPathAppending(OtlpExporterOptions options)
  {
    PropertyInfo? property = typeof(OtlpExporterOptions).GetProperty(
      "AppendSignalPathToEndpoint",
      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    MethodInfo? setter = property?.GetSetMethod(nonPublic: true);
    if (setter is null)
    {
      throw new InvalidOperationException(
        "OpenTelemetry.Exporter.OtlpExporterOptions.AppendSignalPathToEndpoint could not be set, so an http/protobuf secondary endpoint would drop its signal path.");
    }

    setter.Invoke(options, [true]);
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
