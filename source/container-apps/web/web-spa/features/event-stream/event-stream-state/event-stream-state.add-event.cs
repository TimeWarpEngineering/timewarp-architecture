#region Purpose
// AddEvent action set: appends one message to the event stream trace.
#endregion

#region Design
// Sent by EventStreamBehavior for every dispatched action; that behavior must filter this
// Action out before sending or each append would log itself and recurse forever.
// EventList is mutable only through this handler, keeping the on-screen trace an ordered
// record of what actually flowed through the pipeline.
#endregion

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
        CancellationToken cancellationToken
      )
      {
        EventStreamState.EventList.Add(action.Message);
        return Task.CompletedTask;
      }
    }
  }
}
