#region Purpose
// Shared OpenTelemetry meter identity for analytics so the handler and Aspire ServiceDefaults cannot drift.
#endregion

#region Design
// Lives in foundation-contracts/configuration next to ServiceNames: a compile-time name that
// both the TrackEvent handler (web-application) and Aspire ServiceDefaults must see.
// ServiceDefaults stays generic — it AddMeter()s this name and never references a web feature
// project. The handler constructs the Meter with the same constant. Instrument and tag names
// sit here too so tests and the sink cannot drift independently of the meter id.
#endregion

namespace TimeWarp.Foundation.Configuration;

/// <summary>
/// Shared OpenTelemetry meter, instrument, and tag names for analytics events.
/// </summary>
public static class AnalyticsMeters
{
  /// <summary>
  /// Meter name registered by Aspire ServiceDefaults and used by the analytics handler.
  /// </summary>
  public const string MeterName = "TimeWarp.Architecture.Analytics";
  /// <summary>
  /// Counter / histogram instrument name for analytics events.
  /// </summary>
  public const string EventsInstrumentName = "analytics.events";
  /// <summary>
  /// Tag key for the analytics event name.
  /// </summary>
  public const string EventNameTag = "event.name";
  /// <summary>
  /// Tag key for the request correlation id.
  /// </summary>
  public const string CorrelationIdTag = "correlation.id";
}
