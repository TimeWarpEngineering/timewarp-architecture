#region Purpose
// Notification that an action handler produced a user-facing outcome (success or failure) for the shell region.
#endregion

#region Design
// Handlers never dispatch actions (TWS0002), so a multi-step handler such as AddPasskey reports
// "Passkey created." or a browser ceremony failure by publishing this notification; the
// NotificationState handler records the bar. Failures that already are a SharedProblemDetails
// publish ProblemDetailsNotification instead so Title/Detail keep the one shape.
#endregion

namespace TimeWarp.Architecture.Features;

public sealed class OutcomeNotification : INotification
{
  public OutcomeNotification(MessageBarIntent intent, string title, string? body = null)
  {
    Intent = intent;
    Title = title;
    Body = body;
  }

  public MessageBarIntent Intent { get; }
  public string Title { get; }
  public string? Body { get; }
}
