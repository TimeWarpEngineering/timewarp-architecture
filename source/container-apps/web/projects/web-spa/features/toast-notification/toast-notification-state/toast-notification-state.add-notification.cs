#region Purpose
// AddNotification action: records a shell message bar with caller-chosen intent and title.
#endregion

#region Design
// Components dispatch this action instead of injecting INotificationService. The handler
// writes ToastNotificationState; RenderSubscriptionsPostProcessor re-renders subscribers.
// FluentMessageBar in the shell paints the row. No toast provider is required.
#endregion

namespace TimeWarp.Architecture.Features;

partial class ToastNotificationState
{
  // Named ...ActionSet so the TimeWarp.State ActionSetMethodSourceGenerator emits a strongly-typed
  // dispatcher: `ToastNotificationState.AddNotification(intent, title)`.
  public static class AddNotificationActionSet
  {
    public sealed class Action : IBaseAction
    {
      public MessageBarIntent Intent { get; }
      public string Title { get; }

      public Action
      (
        MessageBarIntent intent,
        string title
      )
      {
        Intent = intent;
        Title = title;
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
        ToastNotificationState.AddMessage(action.Intent, action.Title, body: null);
        return ValueTask.CompletedTask;
      }
    }
  }
}
