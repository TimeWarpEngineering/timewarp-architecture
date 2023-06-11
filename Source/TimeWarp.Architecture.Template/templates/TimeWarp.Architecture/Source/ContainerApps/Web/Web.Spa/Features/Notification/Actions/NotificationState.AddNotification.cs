namespace TimeWarp.Architecture.Features.Notifications;

using static TimeWarp.Architecture.Features.Notifications.NotificationState.Notification;

internal partial class NotificationState
{
  internal record AddNotificationAction
  (
    string Title,
    string Message,
    NotificationType Type
  ) : BaseAction;

  internal class AddNotificationHandler : BaseHandler<AddNotificationAction>
  {

    public AddNotificationHandler(IStore aStore) : base(aStore) { }
    public override Task Handle
    (
      AddNotificationAction aAddNotificationAction,
      CancellationToken aCancellationToken
    )
    {
      NotificationState._Notifications.Add
      (
        new Notification
        {
          Title = aAddNotificationAction.Title,
          Message = aAddNotificationAction.Message,
          Type = aAddNotificationAction.Type,
          Id = Guid.NewGuid(),
        }
      );
      return Task.CompletedTask;
    }
  }
}

