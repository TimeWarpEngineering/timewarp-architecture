#region Purpose
// ServiceDefaults OTLP wiring (task 536-001): primary only, secondary only, and both register, and
// an unreachable secondary collector does not fail startup.
#endregion

#region Design
// Host.CreateApplicationBuilder with DisableDefaults so the developer shell's OTEL endpoint is not
// configuration. The facts also set and restore the process environment, because the OTLP options
// constructor reads OTEL_EXPORTER_OTLP_* from the environment. The built exporter does not keep the
// options object. Identity is the OtlpHttpExportClient or OtlpGrpcExportClient under each
// Otlp*Exporter: its Endpoint (port and signal path) and Headers. Logs defer that client behind
// LazyExportClient until the first export, so the reader creates it. The client always adds a
// User-Agent header, so header checks are containment. An http/protobuf endpoint must keep
// v1/traces, v1/metrics, or v1/logs; gRPC must keep the service Export path. The SDK options
// factory binds OTEL_EXPORTER_OTLP_* from host configuration. DisableDefaults leaves the process
// environment out of that configuration, so the helper copies those keys onto the builder.
// Ports 4327 and 4328 are not the SDK defaults (4317 and 4318), so a leaked default exporter is
// visible. These files are Jaribu project tests, not standalone runfiles.
#endregion

namespace Aspire.Tests;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[TestTag("Integration")]
public class SecondaryOtlpExporter_Given_
{
  private const int PrimaryPort = 4327;
  private const int SecondaryPort = 4328;
  private const string PrimaryEndpoint = "http://127.0.0.1:4327";
  private const string SecondaryEndpoint = "http://127.0.0.1:4328";
  private const string PrimaryHeaders = "x-otlp-api-key=primary-secret";
  private const string SecondaryHeaders = "signoz-access-token=secondary-secret";

  private static readonly string[] EnvironmentNames =
  [
    "OTEL_EXPORTER_OTLP_ENDPOINT",
    "OTEL_EXPORTER_OTLP_HEADERS",
    "OTEL_EXPORTER_OTLP_PROTOCOL",
    "OTEL_EXPORTER_OTLP_TIMEOUT",
    "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL",
    "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL",
  ];

  private static readonly SemaphoreSlim EnvironmentGate = new(1, 1);

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SecondaryOtlpExporter_Given_>();

  public static Task PrimaryOnly_Should_RegisterOneExporterPerSignal() =>
    AssertHostAsync(
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = PrimaryEndpoint,
      },
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = PrimaryEndpoint,
        ["OTEL_EXPORTER_OTLP_HEADERS"] = PrimaryHeaders,
        ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc",
      },
      snapshots =>
      {
        AssertSignal(snapshots.Traces, 1, PrimaryPort, "Grpc", PrimaryHeaders);
        AssertSignal(snapshots.Metrics, 1, PrimaryPort, "Grpc", PrimaryHeaders);
        AssertSignal(snapshots.Logs, 1, PrimaryPort, "Grpc", PrimaryHeaders);
      });

  [Input("grpc", "Grpc")]
  [Input("http/protobuf", "HttpProtobuf")]
  [Input("HTTP/Protobuf", "HttpProtobuf")]
  public static Task SecondaryOnly_Should_RegisterOneExporterPerSignal(string protocolText, string protocolName) =>
    AssertHostAsync(
      new Dictionary<string, string?>
      {
        ["Observability:SecondaryOtlp:Endpoint"] = SecondaryEndpoint,
        ["Observability:SecondaryOtlp:Headers"] = SecondaryHeaders,
        ["Observability:SecondaryOtlp:Protocol"] = protocolText,
      },
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_HEADERS"] = PrimaryHeaders,
        ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc",
      },
      snapshots =>
      {
        AssertSignal(snapshots.Traces, 1, SecondaryPort, protocolName, SecondaryHeaders);
        AssertSignal(snapshots.Metrics, 1, SecondaryPort, protocolName, SecondaryHeaders);
        AssertSignal(snapshots.Logs, 1, SecondaryPort, protocolName, SecondaryHeaders);
        snapshots.Traces[0].Headers.ShouldNotContain("primary-secret");
      });

  public static Task Both_Should_RegisterPrimaryAndSecondaryExporters() =>
    AssertHostAsync(
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = PrimaryEndpoint,
        ["Observability:SecondaryOtlp:Endpoint"] = SecondaryEndpoint,
        ["Observability:SecondaryOtlp:Headers"] = SecondaryHeaders,
        ["Observability:SecondaryOtlp:Protocol"] = "http/protobuf",
      },
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = PrimaryEndpoint,
        ["OTEL_EXPORTER_OTLP_HEADERS"] = PrimaryHeaders,
        ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc",
      },
      snapshots =>
      {
        AssertSignal(snapshots.Traces, 2, PrimaryPort, "Grpc", PrimaryHeaders);
        AssertSignal(snapshots.Traces, 2, SecondaryPort, "HttpProtobuf", SecondaryHeaders);
        AssertSignal(snapshots.Metrics, 2, PrimaryPort, "Grpc", PrimaryHeaders);
        AssertSignal(snapshots.Metrics, 2, SecondaryPort, "HttpProtobuf", SecondaryHeaders);
        AssertSignal(snapshots.Logs, 2, PrimaryPort, "Grpc", PrimaryHeaders);
        AssertSignal(snapshots.Logs, 2, SecondaryPort, "HttpProtobuf", SecondaryHeaders);
        ExporterAt(snapshots.Traces, SecondaryPort).Headers.ShouldNotContain("primary-secret");
        ExporterAt(snapshots.Traces, PrimaryPort).Headers.ShouldNotContain("secondary-secret");
      });

  [Input("")]
  [Input("   ")]
  [Input("not-a-url")]
  [Input("ftp://otlp.example/v1")]
  public static Task BlankOrInvalidSecondary_Should_KeepPrimaryOnly(string endpoint) =>
    AssertHostAsync(
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = PrimaryEndpoint,
        ["Observability:SecondaryOtlp:Endpoint"] = endpoint,
      },
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = PrimaryEndpoint,
        ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc",
      },
      snapshots =>
      {
        AssertSignal(snapshots.Traces, 1, PrimaryPort, "Grpc", headers: null);
        AssertSignal(snapshots.Metrics, 1, PrimaryPort, "Grpc", headers: null);
        AssertSignal(snapshots.Logs, 1, PrimaryPort, "Grpc", headers: null);
      });

  public static Task UnreachableSecondary_Should_Start() =>
    AssertHostAsync(
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = PrimaryEndpoint,
        ["Observability:SecondaryOtlp:Endpoint"] = SecondaryEndpoint,
        ["Observability:SecondaryOtlp:Protocol"] = "http/protobuf",
      },
      new Dictionary<string, string?>
      {
        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = PrimaryEndpoint,
        ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc",
      },
      snapshots =>
      {
        snapshots.Traces.Count.ShouldBe(2);
        snapshots.Metrics.Count.ShouldBe(2);
        snapshots.Logs.Count.ShouldBe(2);
      });

  private static void AssertSignal(
    List<SecondaryOtlpExporterGraph.ExporterSnapshot> exporters,
    int count,
    int port,
    string protocol,
    string? headers)
  {
    exporters.Count.ShouldBe(count);
    SecondaryOtlpExporterGraph.ExporterSnapshot exporter = ExporterAt(exporters, port);
    exporter.Protocol.ShouldBe(protocol);
    if (headers is not null)
    {
      exporter.Headers.ShouldContain(headers);
    }

    foreach (SecondaryOtlpExporterGraph.ExporterSnapshot candidate in exporters)
    {
      int? endpointPort = candidate.Endpoint?.Port;
      (endpointPort is 4317 or 4318).ShouldBeFalse("a default OTLP port means an unconfigured exporter leaked in");
    }
  }

  private static SecondaryOtlpExporterGraph.ExporterSnapshot ExporterAt(
    List<SecondaryOtlpExporterGraph.ExporterSnapshot> exporters,
    int port) =>
    exporters.Single(candidate => candidate.Endpoint?.Port == port);

  private static async Task AssertHostAsync(
    Dictionary<string, string?> configuration,
    Dictionary<string, string?> environment,
    Action<SecondaryOtlpExporterGraph.SignalSnapshots> assert)
  {
    await EnvironmentGate.WaitAsync();
    Dictionary<string, string?> previous = Capture();
    try
    {
      if (!environment.ContainsKey("OTEL_EXPORTER_OTLP_TIMEOUT"))
      {
        environment["OTEL_EXPORTER_OTLP_TIMEOUT"] = "1000";
      }

      foreach ((string key, string? value) in environment)
      {
        if (key.StartsWith("OTEL_", StringComparison.Ordinal) && !configuration.ContainsKey(key))
        {
          configuration[key] = value;
        }
      }

      Replace(environment);
      CapturingLoggerProvider capturing = new();
      HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
      {
        DisableDefaults = true,
        EnvironmentName = Environments.Production,
      });
      foreach ((string key, string? value) in configuration)
      {
        builder.Configuration[key] = value;
      }

      builder.Logging.AddProvider(capturing);
      builder.AddServiceDefaults();

      using IHost host = builder.Build();
      await host.StartAsync();
      SecondaryOtlpExporterGraph.SignalSnapshots snapshots = SecondaryOtlpExporterGraph.Read(host);
      assert(snapshots);
      foreach (string line in capturing.Lines)
      {
        line.ShouldNotContain("primary-secret");
        line.ShouldNotContain("secondary-secret");
      }

      await host.StopAsync();
    }
    finally
    {
      Restore(previous);
      EnvironmentGate.Release();
    }
  }

  private static Dictionary<string, string?> Capture()
  {
    Dictionary<string, string?> captured = [];
    foreach (string name in EnvironmentNames)
    {
      captured[name] = Environment.GetEnvironmentVariable(name);
    }

    return captured;
  }

  private static void Replace(IReadOnlyDictionary<string, string?> values)
  {
    foreach (string name in EnvironmentNames)
    {
      Environment.SetEnvironmentVariable(name, null);
    }

    foreach ((string name, string? value) in values)
    {
      Environment.SetEnvironmentVariable(name, value);
    }
  }

  private static void Restore(Dictionary<string, string?> captured)
  {
    foreach ((string name, string? value) in captured)
    {
      Environment.SetEnvironmentVariable(name, value);
    }
  }

  private sealed class CapturingLoggerProvider : ILoggerProvider
  {
    public List<string> Lines { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Lines);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string categoryName, List<string> lines) : ILogger
    {
      public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

      public bool IsEnabled(LogLevel logLevel) => true;

      public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
      {
        if (categoryName.StartsWith("OpenTelemetry", StringComparison.Ordinal))
        {
          return;
        }

        lines.Add(categoryName + " " + formatter(state, exception));
      }
    }
  }
}
