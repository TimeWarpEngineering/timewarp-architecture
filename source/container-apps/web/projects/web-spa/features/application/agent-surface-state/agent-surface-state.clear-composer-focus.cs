#region Purpose
// Drops the one-shot composer focus request the last Ask open raised.
#endregion

#region Design
// AgentAsk dispatches it once the composer took focus, or once the panel settled in a state with no
// composer (signed out, not configured, error), so a later successful build does not pull focus
// after the person moved on (task 381). Clearing an already clear request does nothing.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class ClearComposerFocusActionSet
  {
    [CommandPaletteOptOut("Focus plumbing: the open Ask panel clears its own focus request; a person never needs to.")]
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AgentSurfaceState.ComposerFocusPending = false;
        return default;
      }
    }
  }
}
