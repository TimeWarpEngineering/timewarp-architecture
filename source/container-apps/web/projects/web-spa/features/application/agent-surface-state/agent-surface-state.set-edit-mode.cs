#region Purpose
// Sets whether this conversation asks before editing or edits automatically.
#endregion

#region Design
// The panel rebuilds its in-app functions when the mode changes, because the approval wrapper
// is chosen when the functions are created. WebMCP reads the mode again at invoke time.
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
