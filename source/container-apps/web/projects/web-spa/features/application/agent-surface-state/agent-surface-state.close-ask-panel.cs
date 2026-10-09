#region Purpose
// Closes the docked Ask panel and returns it to the docked width.
#endregion

#region Design
// Closing does not end the conversation. The credential, edit mode, and transcript generation
// stay so the next open continues the same conversation until New conversation or expiry.
// Expand is cleared so the next open docks beside the page instead of covering it.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class CloseAskPanelActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AgentSurfaceState.IsPanelOpen = false;
        AgentSurfaceState.IsPanelExpanded = false;
        return default;
      }
    }
  }
}
