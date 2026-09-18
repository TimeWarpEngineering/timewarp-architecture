#region Purpose
// Turns pipeline-published exception notifications into error toasts.
#endregion

#region Design
// A plain INotificationHandler rather than an ActionSet: exceptions are published by pipeline
// middleware, not dispatched by components, so there is no user action to model.
// Nested in the state partial for feature cohesion even though it touches no state; middleware
// already logs the exception, leaving display as this handler's sole responsibility.
// ShowToastAsync throws FluentServiceProviderException when FluentToastProvider is not in the
// tree (headless SPA tests). Swallow that — the exception is already logged.
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
      try
      {
        await ToastService.ShowToastAsync(options =>
        {
          options.Intent = ToastIntent.Error;
          options.Title = exceptionNotification.Exception.Message;
        });
      }
      catch (FluentServiceProviderException<FluentToastProvider>)
      {
        // Headless hosts register INotificationService without a FluentToastProvider in the tree.
      }
    }
  }
}
