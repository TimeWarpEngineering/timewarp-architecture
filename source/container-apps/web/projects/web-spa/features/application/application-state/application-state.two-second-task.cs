#region Purpose
// ApplicationState action demonstrating a second tracked long-running task (two-second delay).
#endregion

#region Design
// Counterpart to FiveSecondTaskActionSet so the template can demonstrate multiple tracked
// actions running concurrently; [TrackAction] gives each its own busy indicator.
// Public ApplicationState.TwoSecondTask(...) is emitted by TimeWarp.State's ActionSet method
// source generator (do not hand-write a wrapper here — it collides with the generated member,
// and a hand-written one dispatches via a raw Sender.Send, which TWA0022 bans).
// Surface: Developer-gated TestPage "Try it" card (relocated from Home in 147-005).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public static class TwoSecondTaskActionSet
  {
    [TrackAction]
    internal sealed class Action : IAction;

    internal sealed class Handler : ActionHandler<Action>
    {
      public Handler(IStore store) : base(store) {}
      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        Console.WriteLine("Start two Second Task");
        await Task.Delay(millisecondsDelay: 2000, cancellationToken: cancellationToken);
        Console.WriteLine("Two Second Task Complete");
      }
    }
  }
}
