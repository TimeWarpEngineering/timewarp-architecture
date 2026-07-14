#region Purpose
// Interactive demo handlers for the Style Guide page: trigger toasts and errors on demand.
#endregion

#region Design
// The style guide is a living reference, so demos must exercise production code paths
// (state action sets, mediator behaviors) rather than shortcut service calls — what the
// page shows is exactly what real features get.
// The markup gallery lives in the .razor file; this partial holds only behavior.
#endregion

namespace TimeWarp.Architecture.Pages;

[Page("/StyleGuide")]
[CrossFeatureReference("The living style guide deliberately exercises other features' pipelines (counter's throw-exception, toast notifications) so demos run production paths.")]
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
