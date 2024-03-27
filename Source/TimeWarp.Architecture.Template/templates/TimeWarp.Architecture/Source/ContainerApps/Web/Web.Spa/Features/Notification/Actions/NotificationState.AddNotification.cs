namespace TimeWarp.Architecture.Features.Notifications;

using static NotificationState.Notification;

internal sealed partial class NotificationState
{
  [UsedImplicitly]
  public static class AddNotification
  {

    [UsedImplicitly]
    internal sealed class Action
    (
      string Title,
      string Message,
      NotificationType Type
    ) : BaseAction
    {
      public string Title { get; set; } = Title;
      public string Message { get; set; } = Message;
      public NotificationType Type { get; set; } = Type;
    }

    [UsedImplicitly]
    internal sealed class Handler
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
