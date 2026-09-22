#region Purpose
// DismissMessage action: removes one shell message bar by id.
#endregion

#region Design
// FluentMessageBar's built-in dismiss only flips a local Visible flag and does not notify
// state, so a later re-render would show the bar again. The shell button dispatches this
// action; the handler mutates the list and the pipeline re-renders subscribers.
#endregion

namespace TimeWarp.Architecture.Features;

partial class ToastNotificationState
{
  public static class DismissMessageActionSet
  {
    public sealed class Action : IBaseAction
    {
      public string Id { get; }

      public Action(string id)
      {
        Id = id;
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
        ToastNotificationState.RemoveMessage(action.Id);
        return ValueTask.CompletedTask;
      }
    }
  }
}
