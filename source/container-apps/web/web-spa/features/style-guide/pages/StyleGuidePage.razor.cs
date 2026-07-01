namespace TimeWarp.Architecture.Pages;

[Page("/StyleGuide")]
partial class StyleGuidePage
{
  // Toasts go through OUR ToastNotificationState pipeline (the generated ActionSet dispatcher),
  // not a direct INotificationService call — the point is to exercise the app's own path.
  private async Task ShowToast(ToastIntent intent, string title) =>
    await ToastNotificationState.AddNotification(intent, title, CancellationToken);

  // Exercises the auto-toast-on-error path end-to-end: a handler throws ->
  // StateTransactionBehavior publishes ExceptionNotification -> ExceptionNotificationHandler shows a toast.
  private async Task TriggerException() =>
    await Mediator.Send
    (
      new CounterState.ThrowException.Action(Message: "Demo exception dispatched from the Style Guide."),
      CancellationToken
    );
}
