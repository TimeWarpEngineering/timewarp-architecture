#region Purpose
// ApplicationState action demonstrating a tracked long-running task (five-second delay).
#endregion

#region Design
// Template demo, not product behavior: [TrackAction] surfaces the in-flight action via
// TimeWarp.State action tracking so UI can render busy indicators while it runs.
// Public ApplicationState.FiveSecondTask(...) is emitted by TimeWarp.State's ActionSet method
// source generator — the hand-written wrapper that predated the generator was removed with
// TWA0022 (task 196), since it dispatched via a raw Sender.Send. Action is a plain sealed class
// (not a record) to match TwoSecondTaskActionSet, the shape the generator emits from.
// The generated wrapper links an optional caller token with the state's own CancellationToken
// so either the caller or component disposal can cancel the delay.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public static class FiveSecondTaskActionSet
  {
    [TrackAction]
    internal sealed class Action : IAction;

    internal sealed class Handler : ActionHandler<Action>
    {
      public Handler(IStore store) : base(store) {}

      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        Console.WriteLine("Start five second task");
        await Task.Delay(millisecondsDelay: 5000, cancellationToken: cancellationToken);
        Console.WriteLine("Five second task complete");
      }
    }
  }
}
