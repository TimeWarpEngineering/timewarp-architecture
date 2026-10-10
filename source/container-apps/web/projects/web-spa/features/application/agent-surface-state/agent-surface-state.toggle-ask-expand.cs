#region Purpose
// Toggles the Ask panel between the docked width and full screen.
#endregion

#region Design
// Expand is presentational chrome on the same panel. It does not change tools, edit mode, or
// the conversation credential.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class ToggleAskExpandActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AgentSurfaceState.IsPanelExpanded = !AgentSurfaceState.IsPanelExpanded;
        return default;
      }
    }
  }
}
