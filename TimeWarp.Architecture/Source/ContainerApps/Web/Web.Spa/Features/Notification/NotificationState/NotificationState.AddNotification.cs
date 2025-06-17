namespace TimeWarp.Architecture.Features.Notifications;

using static NotificationState.Notification;

partial class NotificationState
{
  public static class AddNotification
  {

    internal sealed class Action
    (
      string Title,
      string Message,
      NotificationType Type
    ) : IBaseAction
    {
      public string Title { get; set; } = Title;
      public string Message { get; set; } = Message;
      public NotificationType Type { get; set; } = Type;
    }

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
