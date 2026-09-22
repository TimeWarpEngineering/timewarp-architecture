#region Purpose
// Turns pipeline-published exception notifications into shell error message bars.
#endregion

#region Design
// A plain INotificationHandler rather than an ActionSet: exceptions are published by pipeline
// middleware, not dispatched by components, so there is no user action to model.
// Nested in the state partial for feature cohesion. The handler mutates ToastNotificationState
// and asks Subscriptions to re-render — it does not Send (TWS0002) and does not call
// INotificationService, which throws unless a FluentToastProvider or FluentMessageBarProvider
// is in the render tree. Headless hosts have neither.
#endregion

namespace TimeWarp.Architecture.Features;

partial class ToastNotificationState
{
  internal class ExceptionNotificationHandler
  (
    IStore store,
    Subscriptions subscriptions
  ) : INotificationHandler<ExceptionNotification>
  {
    public Task Handle
    (
      ExceptionNotification exceptionNotification,
      CancellationToken cancellationToken
    )
    {
      _ = cancellationToken;
      store.GetState<ToastNotificationState>().AddMessage
      (
        MessageBarIntent.Error,
        exceptionNotification.Exception.Message,
        body: null
      );
      subscriptions.ReRenderSubscribers<ToastNotificationState>();
      return Task.CompletedTask;
    }
  }
}
