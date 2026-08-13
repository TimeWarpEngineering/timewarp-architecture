#region Purpose
// AddEvent action set: appends one message to the event stream trace.
#endregion

#region Design
// Dispatched by EventStreamBehavior for every action that flows through the pipeline; that
// behavior must filter this Action out before dispatching or each append would log itself and
// recurse forever.
// EventList is mutable only through this handler, keeping the on-screen trace an ordered
// record of what actually flowed through the pipeline.
// Named ...ActionSet with an explicit Action constructor so the TimeWarp.State
// ActionSetMethodSourceGenerator emits `EventStreamState.AddEvent(message)` — the generator
// reads ConstructorDeclarationSyntax only, so a primary constructor (or an object initializer
// over required members) yields no usable dispatcher. The behavior dispatches through that
// generated method: TWA0022 bans direct mediator Send in SPA client code.
#endregion

namespace TimeWarp.Architecture.Features.EventStreams;

partial class EventStreamState
{
  public static class AddEventActionSet
  {

    internal sealed class Action : IBaseAction
    {
      public string Message { get; }

      public Action(string message)
      {
        Message = message;
      }
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
