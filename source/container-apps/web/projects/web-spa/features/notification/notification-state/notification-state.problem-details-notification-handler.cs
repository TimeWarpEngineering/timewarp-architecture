#region Purpose
// Turns published API problem-details notifications into shell error message bars.
#endregion

#region Design
// The HandleError path: DefaultApiHandler (and any custom handler) publishes, this
// INotificationHandler records a bar in the one shape — problem Title → Title, Detail →
// Body (FromProblem). OperationCancelled (499) is ignored — user-initiated cancellation is
// not an error. Nested in the state partial for feature cohesion. Mutates state and
// re-renders subscribers. Does not Send (TWS0002) and does not call INotificationService,
// so a headless host with no message-bar provider still completes.
#endregion

namespace TimeWarp.Architecture.Features;

partial class NotificationState
{
  internal sealed class ProblemDetailsNotificationHandler
  (
    IStore store,
    Subscriptions subscriptions
  ) : INotificationHandler<ProblemDetailsNotification>
  {
    public Task Handle
    (
      ProblemDetailsNotification problemDetailsNotification,
      CancellationToken cancellationToken
    )
    {
      _ = cancellationToken;
      SharedProblemDetails problem = problemDetailsNotification.SharedProblemDetails;
      if (problem.Status == Constants.OperationCancelled)
      {
        return Task.CompletedTask;
      }

      (string title, string? body) = FromProblem(problem);
      store.GetState<NotificationState>().AddMessage(MessageBarIntent.Error, title, body, DateTimeOffset.UtcNow);
      subscriptions.ReRenderSubscribers<NotificationState>();
      return Task.CompletedTask;
    }
  }
}
