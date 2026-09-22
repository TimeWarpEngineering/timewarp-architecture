#region Purpose
// Turns published API problem-details notifications into shell error message bars.
#endregion

#region Design
// Replaces a toast action as the HandleError path: handlers publish, this
// INotificationHandler records a FluentMessageBar row. OperationCancelled (499) is ignored —
// user-initiated cancellation is not an error. Nested in the state partial for feature cohesion.
// Mutates state and re-renders subscribers. Does not Send (TWS0002) and does not call
// INotificationService, so a headless host with no message-bar provider still completes.
#endregion

namespace TimeWarp.Architecture.Features;

partial class ToastNotificationState
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
      if (problemDetailsNotification.SharedProblemDetails.Status == Constants.OperationCancelled)
      {
        return Task.CompletedTask;
      }

      string message = problemDetailsNotification.SharedProblemDetails.Detail ?? "An error occurred";
      store.GetState<ToastNotificationState>().AddMessage(MessageBarIntent.Error, message, body: null);
      subscriptions.ReRenderSubscribers<ToastNotificationState>();
      return Task.CompletedTask;
    }
  }
}
