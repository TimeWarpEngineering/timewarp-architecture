#region Purpose
// Shared GenAI ActivitySource and meter name so the xAI chat client and Aspire ServiceDefaults cannot drift.
#endregion

#region Design
// Lives in foundation-contracts/configuration next to AnalyticsMeters. ServiceDefaults AddSource/AddMeter
// this name and never references a web feature. Microsoft.Extensions.AI 11.0.1 UseOpenTelemetry uses
// the sourceName argument for both the ActivitySource and the Meter. When that argument is omitted
// both are Experimental.Microsoft.Extensions.AI. The chat factory passes SourceName so export is
// limited to this name.
#endregion

namespace TimeWarp.Foundation.Configuration;

/// <summary>
/// Shared OpenTelemetry identity for Microsoft.Extensions.AI chat spans and token metrics.
/// </summary>
public static class GenAiTelemetry
{
  /// <summary>
  /// ActivitySource name passed to <c>UseOpenTelemetry</c> and registered by Aspire ServiceDefaults.
  /// </summary>
  public const string SourceName = "TimeWarp.Architecture.GenAI";

  /// <summary>
  /// Meter name for the same pipeline. Microsoft.Extensions.AI 11.0.1 uses <see cref="SourceName"/>
  /// for the meter when that string is the <c>sourceName</c> argument.
  /// </summary>
  public const string MeterName = SourceName;
}
