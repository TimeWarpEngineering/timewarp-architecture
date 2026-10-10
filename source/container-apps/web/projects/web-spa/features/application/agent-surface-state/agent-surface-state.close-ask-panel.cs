#region Purpose
// Closes the docked Ask panel, returns it to the docked width, and resets the edit mode.
#endregion

#region Design
// Closing does not end the conversation. The credential and the generation stay, and the
// transcript lives in AskConversationThreads under that generation, so the next open restores the
// same turns until New conversation or expiry. The edit mode does not stay: it returns to
// AskBeforeEditing. EditMode is one value both drivers read (the in-app chat and WebMCP), so
// Automatically edit would otherwise keep removing the WebMCP confirm bar after the person stopped
// watching the panel. Automatic is bounded to an open panel. Expand is cleared so the next open
// docks beside the page instead of covering it.
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
        AgentSurfaceState.EditMode = AgentEditMode.AskBeforeEditing;
        return default;
      }
    }
  }
}
