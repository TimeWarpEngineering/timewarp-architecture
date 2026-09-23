#region Purpose
// Turns handler-published OutcomeNotification into a shell message bar.
#endregion

#region Design
// Mirrors ProblemDetailsNotificationHandler: mutate NotificationState, re-render subscribers,
// never Send (TWS0002), never touch INotificationService. Intent/Title/Body pass straight
// through AddMessage, which dedupes and stamps Success auto-dismiss.
#endregion

namespace TimeWarp.Architecture.Features;

partial class NotificationState
{
  internal sealed class OutcomeNotificationHandler
  (
    IStore store,
    Subscriptions subscriptions
  ) : INotificationHandler<OutcomeNotification>
  {
    public Task Handle
    (
      OutcomeNotification outcomeNotification,
      CancellationToken cancellationToken
    )
    {
      _ = cancellationToken;
      store.GetState<NotificationState>().AddMessage
      (
        outcomeNotification.Intent,
        outcomeNotification.Title,
        outcomeNotification.Body,
        DateTimeOffset.UtcNow
      );
      subscriptions.ReRenderSubscribers<NotificationState>();
      return Task.CompletedTask;
    }
  }
}
