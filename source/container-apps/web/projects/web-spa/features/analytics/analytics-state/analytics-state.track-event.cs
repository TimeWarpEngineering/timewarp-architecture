#region Purpose
// TrackEvent action set: POSTs EventName + CorrelationId to the analytics endpoint.
#endregion

#region Design
// The client is intentionally dumb: event name + correlation id only — no payload, no vendor SDK.
// The server decides the sink. Dispatched by TrackEventBehavior via the generated
// AnalyticsState.TrackEvent(eventName) method (TWA0022 bans direct Mediator.Send). Named
// ...ActionSet with an explicit Action constructor so ActionSetMethodSourceGenerator emits
// that method — the generator reads ConstructorDeclarationSyntax only.
// Does not use DefaultApiHandler: that path toasts SharedProblemDetails, and analytics
// failures must never surface. HTTP exceptions and problem-details arms are logged at Debug
// and swallowed so the traced action is not failed or blocked.
#endregion

namespace TimeWarp.Architecture.Features.Analytics;

using static TrackEvent;

partial class AnalyticsState
{
  public static class TrackEventActionSet
  {
    internal sealed class Action : IBaseAction
    {
      public string EventName { get; }

      public Action(string eventName)
      {
        EventName = eventName;
      }
    }

    internal sealed class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ILogger<Handler> logger
    ) : BaseHandler<Action>(store)
    {
      public override async Task Handle
      (
        Action action,
        CancellationToken cancellationToken
      )
      {
        Command command = new()
        {
          EventName = action.EventName,
          CorrelationId = AnalyticsState.CorrelationId
        };

        try
        {
          OneOf<Response, FileResponse, SharedProblemDetails> apiResponse =
            await webServerApiService.GetResponse<Response>(command, cancellationToken);

          apiResponse.Switch
          (
            _ => { },
            _ => logger.LogDebug(
              "TrackEvent POST returned a file response for {EventName}; ignored.",
              action.EventName),
            problemDetails => logger.LogDebug(
              "TrackEvent POST returned problem {Status} for {EventName}.",
              problemDetails.Status,
              action.EventName)
          );
        }
        catch (Exception exception)
        {
          logger.LogDebug(
            exception,
            "TrackEvent POST failed for {EventName}.",
            action.EventName);
        }
      }
    }
  }
}
