#region Purpose
// AddNotification action: records a shell message bar with caller-chosen intent, title, and body.
#endregion

#region Design
// Components dispatch this action instead of injecting INotificationService. The handler
// writes NotificationState (dedupe + auto-dismiss stamping live in AddMessage);
// RenderSubscriptionsPostProcessor re-renders subscribers and the shell MessageBars host
// paints the row. Title is the operation's own sentence ("Passkey added."), never a
// generic "Error"/"Success" prefix; Body is optional detail. For a SharedProblemDetails use
// ReportProblem so Title/Detail land in Title/Body consistently.
// Body defaults to "" rather than null: the TimeWarp.State ActionSet method generator copies
// the constructor signature but drops the nullable annotation, so `string? body = null`
// becomes `string body = null` inside a #nullable enable file and fails CS8625. AddMessage
// treats whitespace as "no body".
#endregion

namespace TimeWarp.Architecture.Features;

partial class NotificationState
{
  // Named ...ActionSet so the TimeWarp.State ActionSetMethodSourceGenerator emits a strongly-typed
  // dispatcher: `NotificationState.AddNotification(intent, title, body)`.
  public static class AddNotificationActionSet
  {
    public sealed class Action : IBaseAction
    {
      public MessageBarIntent Intent { get; }
      public string Title { get; }
      public string Body { get; }

      public Action
      (
        MessageBarIntent intent,
        string title,
        string body = ""
      )
      {
        Intent = intent;
        Title = title;
        Body = body;
      }
    }

    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle
      (
        Action action,
        CancellationToken cancellationToken
      )
      {
        _ = cancellationToken;
        NotificationState.AddMessage(action.Intent, action.Title, action.Body, DateTimeOffset.UtcNow);
        return ValueTask.CompletedTask;
      }
    }
  }
}
