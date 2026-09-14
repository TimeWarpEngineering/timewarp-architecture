#region Purpose
// Pipeline behavior that POSTs opted-in SPA actions to the analytics endpoint after they succeed.
#endregion

#region Design
// The client is intentionally dumb: event name + correlation id only — no payload, no vendor SDK.
// The server decides the sink. Actions opt in via [TrackEvent] rather than tracking every action
// by default. A pipeline behavior (not a post-action notification) is used so the template keeps
// a client IPipelineBehavior exemplar after event-stream is retired.
// After await next() succeeds, typeof(TRequest) is checked for [TrackEvent]; dispatch goes
// through the generated AnalyticsState.TrackEvent(...) method off IStore (TWA0022 bans direct
// Mediator.Send). Nested Action types all have Name "Action", so the wire EventName is
// FullName. TrackEventActionSet.Action is skipped to avoid recursion.
// Teardown semantics match EventStreamBehavior: State<TState>.Dispose cancels then disposes
// the CancellationTokenSource, so a post-disposal generated dispatch raises
// ObjectDisposedException (not a mere cancellation). OperationCanceledException is kept as
// forward cover. Losing a telemetry POST during teardown must never fail the traced action.
// Constrained to IAction so non-state mediator requests are not inspected.
#endregion

namespace TimeWarp.Architecture.Features.Analytics;

using static AnalyticsState;
using Guard = Ardalis.GuardClauses.Guard;

/// <summary>
/// After a successful <see cref="IAction"/>, posts the request type name when it is marked
/// <see cref="TrackEventAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The pipeline request type.</typeparam>
/// <typeparam name="TResponse">The pipeline response type.</typeparam>
/// <remarks>Skip <see cref="TrackEventActionSet.Action"/> or the behavior would recurse forever.</remarks>
public class TrackEventBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull, IAction
{
  private readonly ILogger Logger;
  private readonly IStore Store;

  public TrackEventBehavior
  (
    ILogger<TrackEventBehavior<TRequest, TResponse>> logger,
    IStore store
  )
  {
    Logger = logger;
    Store = store;
  }

  public async Task<TResponse> Handle
  (
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken
  )
  {
    Guard.Against.Null(next);

    TResponse response = await next().ConfigureAwait(false);
    await TryTrackAsync(request).ConfigureAwait(false);
    return response;
  }

  private async Task TryTrackAsync(TRequest request)
  {
    if (request is TrackEventActionSet.Action)
    {
      return;
    }

    if (typeof(TRequest).GetCustomAttribute<TrackEventAttribute>() is null)
    {
      return;
    }

    string eventName = request.GetType().FullName ?? request.GetType().Name;

    try
    {
      await Store.GetState<AnalyticsState>().TrackEvent(eventName);
    }
    catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)
    {
      Logger.LogDebug("TrackEvent '{EventName}' dropped — state disposed.", eventName);
    }
  }
}
