namespace TimeWarp.Architecture.Features.EventStreams;

internal partial class EventStreamState
{
  public static class AddEvent
  {

    internal sealed class Action : BaseAction
    {
      public required string Message { get; init; }
    }

    [UsedImplicitly]
    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {

      public override Task Handle
      (
        Action action,
        CancellationToken aCancellationToken
      )
      {
        EventStreamState.EventList.Add(action.Message);
        return Task.CompletedTask;
      }
    }
  }
}
