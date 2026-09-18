#region Purpose
// Turns published API problem-details notifications into error toasts.
#endregion

#region Design
// Replaces AddProblemDetailsActionSet as the HandleError path: handlers publish, this
// INotificationHandler displays. OperationCancelled (499) is swallowed — user-initiated
// cancellation is not an error toast. Nested in the state partial for feature cohesion.
// ShowToastAsync throws FluentServiceProviderException when FluentToastProvider is not in the
// tree (headless SPA tests). Swallow that — the problem is already in the notification.
#endregion

namespace TimeWarp.Architecture.Features;

partial class ToastNotificationState
{
  internal sealed class ProblemDetailsNotificationHandler
  (
    INotificationService ToastService
  ) : INotificationHandler<ProblemDetailsNotification>
  {
    public async Task Handle
    (
      ProblemDetailsNotification problemDetailsNotification,
      CancellationToken cancellationToken
    )
    {
      _ = cancellationToken;
      if (problemDetailsNotification.SharedProblemDetails.Status == Constants.OperationCancelled)
      {
        return;
      }

      string message = problemDetailsNotification.SharedProblemDetails.Detail ?? "An error occurred";
      try
      {
        await ToastService.ShowToastAsync(options =>
        {
          options.Intent = ToastIntent.Error;
          options.Title = message;
        });
      }
      catch (FluentServiceProviderException<FluentToastProvider>)
      {
        // Headless hosts register INotificationService without a FluentToastProvider in the tree.
      }
    }
  }
}
