namespace TimeWarp.Architecture.Features.EventStreams;

internal partial class EventStreamState
{
  internal class AddEventHandler : BaseHandler<AddEventAction>
  {
    public AddEventHandler(IStore aStore) : base(aStore) { }

    public override Task Handle
    (
      AddEventAction aAddEventAction,
      CancellationToken aCancellationToken
    )
    {
      EventStreamState._Events.Add(aAddEventAction.Message);
      return Task.CompletedTask;
    }
  }
}
