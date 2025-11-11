namespace TimeWarp.Architecture.Features.EventStreams;

using static EventStreamState;
using Guard=Ardalis.GuardClauses.Guard;

/// <summary>
/// Every event that comes through the pipeline adds an object to the EventStreamState
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
/// <remarks>To avoid infinite recursion don't add AddEvent to the event stream</remarks>
public class EventStreamBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull, IAction
{
  private readonly ILogger Logger;
  private readonly ISender Sender;
  public Guid Guid { get; } = Guid.NewGuid();

  public EventStreamBehavior
  (
    ILogger<EventStreamBehavior<TRequest, TResponse>> logger,
    ISender sender
  )
  {
    Logger = logger;
    Sender = sender;
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
    if (request is not AddEvent.Action) //Skip to avoid recursion
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

      var addEventAction = new AddEvent.Action(){ Message = message};
      await Sender.Send(addEventAction);
    }
  }
}
