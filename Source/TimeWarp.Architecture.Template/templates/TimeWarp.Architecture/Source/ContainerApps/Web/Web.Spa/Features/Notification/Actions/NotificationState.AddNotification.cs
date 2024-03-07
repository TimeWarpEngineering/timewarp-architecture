namespace TimeWarp.Architecture.Features.Notifications;

using static TimeWarp.Architecture.Features.Notifications.NotificationState.Notification;

internal partial class NotificationState
{
  public static class AddNotification
  {

    internal record Action
    (
      string Title,
      string Message,
      NotificationType Type
    ) : BaseAction;

    internal class Handler : BaseHandler<Action>
    {

      public Handler(IStore store) : base(store) { }
      public override Task Handle
      (
        Action action,
        CancellationToken aCancellationToken
      )
      {
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

