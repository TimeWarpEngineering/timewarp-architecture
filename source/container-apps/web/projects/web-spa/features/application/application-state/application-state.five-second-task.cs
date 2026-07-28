#region Purpose
// ApplicationState action demonstrating a tracked long-running task (five-second delay).
#endregion

#region Design
// Template demo, not product behavior: [TrackAction] surfaces the in-flight action via
// TimeWarp.State action tracking so UI can render busy indicators while it runs.
// The public wrapper links an optional caller token with the state's own CancellationToken
// so either the caller or component disposal can cancel the delay.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public static class FiveSecondTaskActionSet
  {
    [TrackAction]
    internal sealed record Action : IAction;

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

  public async Task FiveSecondTask(CancellationToken? externalCancellationToken = null)
  {
    using CancellationTokenSource? linkedCts = externalCancellationToken.HasValue
      ? CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken.Value, CancellationToken)
      : null;

    await Sender.Send
    (
      new FiveSecondTaskActionSet.Action(),
      linkedCts?.Token ?? CancellationToken
    );
  }
}
