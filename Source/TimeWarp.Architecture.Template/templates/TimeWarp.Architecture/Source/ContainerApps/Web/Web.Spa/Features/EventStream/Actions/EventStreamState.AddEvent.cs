namespace TimeWarp.Architecture.Features.EventStreams;

internal partial class EventStreamState
{
  public static class AddEvent
  {

    internal record Action : BaseAction
    {
      public required string Message { get; init; }
    }

    [UsedImplicitly]
    internal class Handler
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
        EventStreamState._Events.Add(action.Message);
        return Task.CompletedTask;
      }
    }
  }
}
