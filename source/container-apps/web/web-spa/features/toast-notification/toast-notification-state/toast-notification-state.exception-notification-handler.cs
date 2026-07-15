#region Purpose
// Turns pipeline-published exception notifications into error toasts.
#endregion

#region Design
// A plain INotificationHandler rather than an ActionSet: exceptions are published by pipeline
// middleware, not dispatched by components, so there is no user action to model.
// Nested in the state partial for feature cohesion even though it touches no state; middleware
// already logs the exception, leaving display as this handler's sole responsibility.
#endregion

namespace TimeWarp.Architecture.Features;

partial class ToastNotificationState
{

  internal class ExceptionNotificationHandler
  (
    INotificationService ToastService
  ) : INotificationHandler<ExceptionNotification>
  {
    public async Task Handle
    (
      ExceptionNotification exceptionNotification,
      CancellationToken cancellationToken
    )
    {
      // Note: we are not storing the exceptions in state as they are already logged by middleware.
      // If we think we need a log/Notification view we will want to keep them.
      await ToastService.ShowToastAsync(options =>
      {
        options.Intent = ToastIntent.Error;
        options.Title = exceptionNotification.Exception.Message;
      });
    }
  }
}
