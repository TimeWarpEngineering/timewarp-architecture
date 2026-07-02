#region Purpose
// ApplicationState action demonstrating a second tracked long-running task (two-second delay).
#endregion

#region Design
// Counterpart to FiveSecondTaskActionSet so the template can demonstrate multiple tracked
// actions running concurrently; [TrackAction] gives each its own busy indicator.
// The commented-out wrapper documents the linked-cancellation dispatch pattern; the action
// can also be sent directly through Sender.
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

  // public async Task TwoSecondTask(CancellationToken? externalCancellationToken = null)
  // {
  //   using CancellationTokenSource? linkedCts = externalCancellationToken.HasValue
  //     ? CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken.Value, CancellationToken)
  //     : null;
  //
  //   await Sender.Send
  //   (
  //     new TwoSecondTaskActionSet.Action(),
  //     linkedCts?.Token ?? CancellationToken
  //   );
  // }
}
