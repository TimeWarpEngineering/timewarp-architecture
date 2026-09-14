#region Purpose
// Server-side TrackEvent sink: emit an OpenTelemetry log event and increment analytics.events.
#endregion

#region Design
// The sink is here — swap or add a vendor (e.g. Segment) in this handler; the client stays dumb.
// OpenTelemetry (structured log event + counter) is the template default so SPA actions show
// up in the Aspire dashboard without a vendor SDK. Always returns success: telemetry
// failures never surface to the caller. Meter name lives in
// TimeWarp.Foundation.Configuration.AnalyticsMeters so Aspire ServiceDefaults can AddMeter it
// without referencing this web feature. The Meter is a process-lifetime static so
// AddMeter-by-name collects it; the handler does not dispose it.
#endregion

namespace TimeWarp.Architecture.Features.Analytics.Application;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using TimeWarp.Foundation.Configuration;
using static TimeWarp.Architecture.Features.Analytics.TrackEvent;

public sealed partial class TrackEvent
{
  public partial class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private static readonly Meter Meter = new(AnalyticsMeters.MeterName);
    private static readonly Counter<long> AnalyticsEvents = Meter.CreateCounter<long>(AnalyticsMeters.EventsInstrumentName);

    private readonly ILogger<Handler> Logger;

    public Handler(ILogger<Handler> logger)
    {
      Logger = logger;
    }

    public Task<OneOf<Response, SharedProblemDetails>> Handle
    (
      Command command,
      CancellationToken cancellationToken
    )
    {
      try
      {
        LogTrackedEvent(Logger, command.EventName, command.CorrelationId);
        AnalyticsEvents.Add
        (
          1,
          new TagList { { AnalyticsMeters.EventNameTag, command.EventName } }
        );
      }
      catch (Exception exception)
      {
        // Telemetry failures never surface to the caller.
        Debug.WriteLine(exception);
      }

      return Task.FromResult((OneOf<Response, SharedProblemDetails>)new Response());
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Analytics event {event.name}")]
    private static partial void LogTrackedEvent
    (
      ILogger logger,
      [TagName(AnalyticsMeters.EventNameTag)] string eventName,
      [TagName(AnalyticsMeters.CorrelationIdTag)] Guid? correlationId
    );
  }
}
