#region Purpose
// One process-environment gate for the secondary OTLP ServiceDefaults and AppHost model tests.
#endregion

#region Design
// Both suites write the same OTEL and Aspire dashboard variables. A gate per class lets those
// writes overlap. IsolateAsync holds one semaphore across the two classes, replaces those
// variables for the body, and restores them before the next fact. The model suite reads the built
// model after the body returns, so the restore has already run; Forward copied the values onto
// the resource callbacks while the body was inside the gate.
#endregion

namespace Aspire.Tests;

/// <summary>Serializes process-environment edits for the secondary OTLP tests.</summary>
internal static class SecondaryOtlpEnvironment
{
  private static readonly SemaphoreSlim Gate = new(1, 1);

  private static readonly string[] Names =
  [
    "OTEL_EXPORTER_OTLP_ENDPOINT",
    "OTEL_EXPORTER_OTLP_HEADERS",
    "OTEL_EXPORTER_OTLP_PROTOCOL",
    "OTEL_EXPORTER_OTLP_TIMEOUT",
    "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL",
    "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL",
  ];

  internal static async Task<T> IsolateAsync<T>(
    IReadOnlyDictionary<string, string?> values,
    Func<Task<T>> body)
  {
    ArgumentNullException.ThrowIfNull(values);
    ArgumentNullException.ThrowIfNull(body);
    await Gate.WaitAsync();
    Dictionary<string, string?> previous = Capture();
    try
    {
      Replace(values);
      return await body();
    }
    finally
    {
      Restore(previous);
      Gate.Release();
    }
  }

  private static Dictionary<string, string?> Capture()
  {
    Dictionary<string, string?> captured = [];
    foreach (string name in Names)
    {
      captured[name] = Environment.GetEnvironmentVariable(name);
    }

    return captured;
  }

  private static void Replace(IReadOnlyDictionary<string, string?> values)
  {
    foreach (string name in Names)
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
}
