#region Purpose
// Turns pipeline-published exception notifications into shell error message bars.
#endregion

#region Design
// A plain INotificationHandler rather than an ActionSet: exceptions are published by pipeline
// middleware, not dispatched by components, so there is no user action to model.
// Nested in the state partial for feature cohesion. The handler mutates NotificationState
// and asks Subscriptions to re-render — it does not Send (TWS0002) and does not call
// INotificationService, which throws unless a FluentToastProvider or FluentMessageBarProvider
// is in the render tree. Headless hosts have neither.
// Shape: the exception message is the Title (there is no separate detail).
#endregion

namespace TimeWarp.Architecture.Features;

partial class NotificationState
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
      store.GetState<NotificationState>().AddMessage
      (
        MessageBarIntent.Error,
        exceptionNotification.Exception.Message,
        body: null,
        DateTimeOffset.UtcNow
      );
      subscriptions.ReRenderSubscribers<NotificationState>();
      return Task.CompletedTask;
    }
  }
}
