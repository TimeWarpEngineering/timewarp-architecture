namespace TimeWarp.Architecture.Features.ToastNotifications;

partial class ToastNotificationState
{
  [UsedImplicitly]
  internal class ExceptionNotificationHandler
  (
    IToastService ToastService
  ) : INotificationHandler<ExceptionNotification>
  {
    public Task Handle
    (
      ExceptionNotification exceptionNotification,
      CancellationToken cancellationToken
    )
    {
      // Note: we are not storing the exceptions in state as they are already logged by middleware.
      // If we think we need a log/Notification view we will want to keep them.
      ToastService.ShowError(exceptionNotification.Exception.Message);
      return Task.CompletedTask;
    }
  }
}
