namespace TimeWarp.Architecture.Features.Notifications;

using static NotificationState.Notification;

internal partial class NotificationState
{
  [UsedImplicitly]
  public static class AddNotification
  {

    [UsedImplicitly]
    internal record Action
    (
      string Title,
      string Message,
      NotificationType Type
    ) : BaseAction;

    [UsedImplicitly]
    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {

      public override Task Handle
      (
        Action action,
        CancellationToken aCancellationToken
      )
      {
        NotificationState.NotificationList ??= [];

        NotificationState.NotificationList.Add
        (
          new Notification
          {
            Title = action.Title,
            Message = action.Message,
            Type = action.Type,
            Id = Guid.NewGuid(),
          }
        );
        return Task.CompletedTask;
      }
    }
  }
}
