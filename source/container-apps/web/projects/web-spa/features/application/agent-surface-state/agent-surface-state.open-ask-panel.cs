#region Purpose
// Opens the docked Ask panel.
#endregion

#region Design
// The panel is shell state, not a modal id, because TimeWarpPage remounts on navigation and a
// modal would not survive as the place the page reflows around. Opening does not mint the
// conversation credential; the panel does that once it can read the signed-in principal.
// Every open path (top-bar button, Ctrl-K row, reopen) ends here, so this is where the composer
// focus request is raised (ComposerFocusPending, task 381). It is raised on every open, so a
// second open focuses too.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class OpenAskPanelActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AgentSurfaceState.IsPanelOpen = true;
        AgentSurfaceState.ComposerFocusPending = true;
        return default;
      }
    }
  }
}
