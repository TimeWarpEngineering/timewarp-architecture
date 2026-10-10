#region Purpose
// Sets whether this conversation asks before editing or edits automatically.
#endregion

#region Design
// One mode for both drivers. The panel rebuilds its in-app functions when the mode changes (after
// any running turn finishes), because the approval wrapper is chosen when the functions are
// created; an unwrapped function refuses if the mode has gone back to Ask meanwhile. WebMCP reads
// the mode again at invoke time. CloseAskPanel and NewConversation reset it to AskBeforeEditing,
// so AutomaticallyEdit only lasts while the panel stays open on one conversation.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class SetEditModeActionSet
  {
    public sealed class Action : IBaseAction
    {
      public AgentEditMode Mode { get; }

      public Action(AgentEditMode mode)
      {
        Mode = mode;
      }
    }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AgentSurfaceState.EditMode = action.Mode;
        return default;
      }
    }
  }
}
