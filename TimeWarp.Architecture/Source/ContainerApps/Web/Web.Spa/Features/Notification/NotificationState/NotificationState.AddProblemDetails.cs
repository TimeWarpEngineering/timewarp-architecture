namespace TimeWarp.Architecture.Features.Notifications;

using static NotificationState.Notification;

partial class NotificationState
{

  public static class AddProblemDetails
  {


    internal sealed class Action
    (
      SharedProblemDetails SharedProblemDetails
    ) : IBaseAction
    {
      public SharedProblemDetails SharedProblemDetails { get; init; } = SharedProblemDetails;
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
