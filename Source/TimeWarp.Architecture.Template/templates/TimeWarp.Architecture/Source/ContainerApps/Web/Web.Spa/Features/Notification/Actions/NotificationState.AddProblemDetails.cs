namespace TimeWarp.Architecture.Features.Notifications;

using static NotificationState.Notification;

internal partial class NotificationState
{
  [UsedImplicitly]
  public static class AddProblemDetails
  {

    [UsedImplicitly]
    internal sealed class Action
    (
      SharedProblemDetails SharedProblemDetails
    ) : BaseAction
    {
      public SharedProblemDetails SharedProblemDetails { get; init; } = SharedProblemDetails;
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

        var notification = new Notification
        {
          Title = action.SharedProblemDetails.Title ?? "Error",
          Message = action.SharedProblemDetails.Detail ?? "An error occurred",
          Type = NotificationType.Error,
          Id = Guid.NewGuid(),
        };

        NotificationState.NotificationList.Add(notification);
        return Task.CompletedTask;
      }
    }
  }
}
