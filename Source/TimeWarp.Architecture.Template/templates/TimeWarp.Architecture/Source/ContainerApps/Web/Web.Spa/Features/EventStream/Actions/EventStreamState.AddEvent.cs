namespace TimeWarp.Architecture.Features.EventStreams;

internal partial class EventStreamState
{
  public static class AddEvent
  {

    internal record Action : BaseAction
    {
      public string Message { get; set; }
    }

    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) { }

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
