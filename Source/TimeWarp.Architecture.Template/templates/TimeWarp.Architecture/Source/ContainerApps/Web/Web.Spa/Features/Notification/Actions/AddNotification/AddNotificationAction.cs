namespace TimeWarp.Architecture.Features.Notifications;

using static TimeWarp.Architecture.Features.Notifications.NotificationState.Notification;

public partial class NotificationState
{
  public record AddNotificationAction
  (
    string Title,
    string Message,
    NotificationType Type
  ) : BaseAction;
}

