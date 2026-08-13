#region Purpose
// Pipeline behavior that records Start/Completed entries for every action into EventStreamState.
#endregion

#region Design
// Gives an in-app, ordered trace of action dispatch (rendered by the EventStream component)
// without requiring Redux DevTools to be attached.
// Writes via the generated EventStreamState.AddEvent(message) ActionSet method (resolved off
// IStore) so the log entry itself flows through the normal state pipeline; TWA0022 bans the
// direct Sender.Send this once used. AddEventActionSet.Action is explicitly skipped or the
// behavior would recurse forever.
// Teardown semantics: the generated method reads the state's own CancellationToken on every call,
// where this previously passed CancellationToken.None. State<TState>.Dispose cancels AND THEN
// disposes the CancellationTokenSource, so after disposal the token cannot be read at all —
// reading CancellationTokenSource.Token throws ObjectDisposedException. That, not a cancellation,
// is what a post-disposal trace dispatch actually raises: nothing on this path observes the token
// (the AddEvent handler ignores its parameter), so a merely cancelled token throws nothing.
// The guard therefore catches ObjectDisposedException, and keeps OperationCanceledException as
// forward cover should a future handler start observing the token. Either way a diagnostic trace
// entry lost during teardown must never fail (or mask the result of) the action it was tracing.
// Constrained to IAction so non-state mediator requests are not traced.
#endregion

namespace TimeWarp.Architecture.Features.EventStreams;

using static EventStreamState;
using Guard=Ardalis.GuardClauses.Guard;

/// <summary>
/// Every event that comes through the pipeline adds an object to the EventStreamState
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
/// <remarks>To avoid infinite recursion don't add AddEventActionSet to the event stream</remarks>
public class EventStreamBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull, IAction
{
  private readonly ILogger Logger;
  private readonly IStore Store;
  public Guid Guid { get; } = Guid.NewGuid();

  public EventStreamBehavior
  (
    ILogger<EventStreamBehavior<TRequest, TResponse>> logger,
    IStore store
  )
  {
    Logger = logger;
    Store = store;
    Logger.LogDebug($"{GetType().Name}: Constructor");
  }

  public async Task<TResponse> Handle
  (
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken
  )
  {
    Guard.Against.Null(next);

    await AddEventToStream(request, tag: "Start").ConfigureAwait(false);
    TResponse newState = await next().ConfigureAwait(false);
    await AddEventToStream(request, tag: "Completed").ConfigureAwait(false);
    return newState;
  }

  private async Task AddEventToStream(TRequest request, string tag)
  {
    if (request is not AddEventActionSet.Action) //Skip to avoid recursion
    {
      string message = $"{tag}:{request.GetType().Name}";

      try
      {
        await Store.GetState<EventStreamState>().AddEvent(message);
      }
      catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)
      {
        // State disposed mid-flight: losing a trace entry is acceptable, failing the traced
        // action is not.
        Logger.LogDebug("Event stream trace '{Message}' dropped — state disposed.", message);
      }
    }
  }
}
