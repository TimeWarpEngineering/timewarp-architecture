namespace TimeWarp.Architecture.Features.ToastNotifications;

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
