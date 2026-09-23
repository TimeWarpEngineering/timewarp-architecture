#region Purpose
// ExpireMessages action: removes auto-dismissing bars whose AutoDismissAt has passed.
#endregion

#region Design
// Success bars carry AutoDismissAt (stamped in AddMessage). The shell MessageBars host
// schedules this action for NextExpiry and passes the clock value it observed, so the state
// never owns a timer (timers do not survive StateTransactionBehavior's clone/rollback) and
// headless tests can expire deterministically by passing a future instant.
#endregion

namespace TimeWarp.Architecture.Features;

partial class NotificationState
{
  public static class ExpireMessagesActionSet
  {
    public sealed class Action : IBaseAction
    {
      public DateTimeOffset Now { get; }

      public Action(DateTimeOffset now)
      {
        Now = now;
      }
    }

    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle
      (
        Action action,
        CancellationToken cancellationToken
      )
      {
        _ = cancellationToken;
        NotificationState.RemoveExpired(action.Now);
        return ValueTask.CompletedTask;
      }
    }
  }
}
