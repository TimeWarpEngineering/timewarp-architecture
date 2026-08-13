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
      string message;
      string requestTypeName = request.GetType().Name;
      if (request is BaseRequest)
      {
        message = $"{tag}:{requestTypeName}";
      }
      else
      {
        message = $"{tag}:{requestTypeName}";
      }

      await Store.GetState<EventStreamState>().AddEvent(message);
    }
  }
}
