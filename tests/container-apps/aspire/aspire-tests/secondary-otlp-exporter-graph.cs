#region Purpose
// Reads the live OpenTelemetry exporter graph for the secondary OTLP ServiceDefaults tests.
#endregion

#region Design
// OtlpTraceExporter and OtlpMetricExporter keep one transmission handler. OtlpLogExporter defers
// its client behind LazyExportClient until the first export. The handler's parent provider also
// reaches the other signals, so the reader uses the handler field instead of a second graph walk.
// Endpoint and Headers live on OtlpExportClient. The header map always includes User-Agent.
#endregion

namespace Aspire.Tests;

using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

internal static class SecondaryOtlpExporterGraph
{
  internal static SignalSnapshots Read(IHost host)
  {
    ArgumentNullException.ThrowIfNull(host);

    // DependencyInjection is a global using only while the web template flag is on.
    TracerProvider traces = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
      .GetRequiredService<TracerProvider>(host.Services);
    MeterProvider metrics = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
      .GetRequiredService<MeterProvider>(host.Services);
    object logs = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
      .GetServices<ILoggerProvider>(host.Services)
      .Single(provider => provider.GetType().Name == "OpenTelemetryLoggerProvider");
    return new SignalSnapshots(
      ReadExporters(traces, "OtlpTraceExporter"),
      ReadExporters(metrics, "OtlpMetricExporter"),
      ReadExporters(logs, "OtlpLogExporter"));
  }

  private static List<ExporterSnapshot> ReadExporters(object root, string exporterTypeName)
  {
    List<ExporterSnapshot> snapshots = [];
    foreach (object exporter in Find(root, exporterTypeName))
    {
      object? handler = ReadMember(exporter, "transmissionHandler");
      if (handler is null)
      {
        throw new InvalidOperationException($"{exporterTypeName} did not carry a transmission handler.");
      }

      object? exportClient = ReadMember(handler, "ExportClient");
      if (exportClient is null)
      {
        throw new InvalidOperationException($"{exporterTypeName} did not carry an export client.");
      }

      object client = ResolveExportClient(exportClient);
      bool http = client.GetType().Name == "OtlpHttpExportClient";
      Uri? endpoint = ReadMember(client, "Endpoint") as Uri;
      string path = endpoint?.AbsolutePath ?? "";
      path.ShouldContain(SignalPathFragment(exporterTypeName, http));
      snapshots.Add(new ExporterSnapshot(
        endpoint,
        http ? "HttpProtobuf" : "Grpc",
        FormatHeaders(ReadMember(client, "Headers"))));
    }

    return snapshots;
  }

  private static object ResolveExportClient(object exportClient)
  {
    if (exportClient.GetType().Name != "LazyExportClient")
    {
      return exportClient;
    }

    object? lazy = ReadMember(exportClient, "exportClient");
    if (lazy is null)
    {
      throw new InvalidOperationException("LazyExportClient did not carry its factory.");
    }

    PropertyInfo? value = lazy.GetType().GetProperty("Value");
    object? created = value?.GetValue(lazy);
    if (created is null)
    {
      throw new InvalidOperationException("LazyExportClient did not create an export client.");
    }

    return created;
  }

  private static string SignalPathFragment(string exporterTypeName, bool http) =>
    (exporterTypeName, http) switch
    {
      ("OtlpTraceExporter", true) => "v1/traces",
      ("OtlpMetricExporter", true) => "v1/metrics",
      ("OtlpLogExporter", true) => "v1/logs",
      ("OtlpTraceExporter", false) => "TraceService/Export",
      ("OtlpMetricExporter", false) => "MetricsService/Export",
      ("OtlpLogExporter", false) => "LogsService/Export",
      _ => throw new InvalidOperationException($"No OTLP signal path is known for {exporterTypeName}."),
    };

  private static object? ReadMember(object target, string name)
  {
    for (Type? current = target.GetType(); current is not null && current != typeof(object); current = current.BaseType)
    {
      PropertyInfo? property = current.GetProperty(
        name,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
      if (property is not null)
      {
        return property.GetValue(target);
      }

      FieldInfo? field = current.GetField(
        name,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
      if (field is not null)
      {
        return field.GetValue(target);
      }
    }

    return null;
  }

  private static string FormatHeaders(object? headers)
  {
    if (headers is not System.Collections.IEnumerable enumerable || headers is string)
    {
      return headers?.ToString() ?? "";
    }

    List<string> pairs = [];
    foreach (object? item in enumerable)
    {
      if (item is System.Collections.DictionaryEntry entry)
      {
        pairs.Add($"{entry.Key}={entry.Value}");
        continue;
      }

      if (item is null)
      {
        continue;
      }

      Type itemType = item.GetType();
      PropertyInfo? key = itemType.GetProperty("Key");
      PropertyInfo? value = itemType.GetProperty("Value");
      if (key is not null && value is not null)
      {
        pairs.Add($"{key.GetValue(item)}={value.GetValue(item)}");
      }
    }

    return string.Join(',', pairs);
  }

  private static List<object> Find(object root, string typeName)
  {
    List<object> found = [];
    HashSet<object> seen = new(ReferenceEqualityComparer.Instance);
    Walk(root, 0, seen, found, typeName);
    return found;
  }

  private static void Walk(object? value, int depth, HashSet<object> seen, List<object> found, string typeName)
  {
    if (value is null || depth > 16 || value is string or Uri or Delegate)
    {
      return;
    }

    Type type = value.GetType();
    if (type.IsPrimitive || type.IsEnum)
    {
      return;
    }

    if (!type.IsValueType && !seen.Add(value))
    {
      return;
    }

    if (type.Name == typeName)
    {
      found.Add(value);
    }

    if (value is System.Collections.IEnumerable enumerable)
    {
      try
      {
        foreach (object? item in enumerable)
        {
          Walk(item, depth + 1, seen, found, typeName);
        }
      }
      catch (InvalidOperationException)
      {
        return;
      }
    }

    for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
    {
      foreach (FieldInfo field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
      {
        if (field.FieldType.IsPointer || field.FieldType == typeof(IntPtr) || field.FieldType == typeof(UIntPtr))
        {
          continue;
        }

        object? fieldValue;
        try
        {
          fieldValue = field.GetValue(value);
        }
        catch (FieldAccessException)
        {
          continue;
        }
        catch (NotSupportedException)
        {
          continue;
        }
        catch (TargetException)
        {
          continue;
        }

        Walk(fieldValue, depth + 1, seen, found, typeName);
      }
    }
  }

  internal sealed record SignalSnapshots(
    List<ExporterSnapshot> Traces,
    List<ExporterSnapshot> Metrics,
    List<ExporterSnapshot> Logs);

  internal sealed record ExporterSnapshot(Uri? Endpoint, string Protocol, string Headers);
}
