namespace TimeWarp.Architecture.Features.EventStreams;

partial class EventStreamState
{
  public static class AddEvent
  {

    internal sealed class Action : IBaseAction
    {
      public required string Message { get; init; }
    }


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
