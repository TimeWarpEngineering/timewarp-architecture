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

public static class AnalyticsMeters
{
  public const string MeterName = "TimeWarp.Architecture.Analytics";
  public const string EventsInstrumentName = "analytics.events";
  public const string EventNameTag = "event.name";
  public const string CorrelationIdTag = "correlation.id";
}
