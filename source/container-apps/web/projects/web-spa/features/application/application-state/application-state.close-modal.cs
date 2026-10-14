#region Purpose
// CloseModal action set: dismisses whichever modal is active.
#endregion

#region Design
// Modal visibility is centralized as the single ActiveModalId on ApplicationState, so closing
// needs no payload — one action dismisses any modal regardless of which component opened it
// (via SetActiveModalActionSet).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{

  public static class CloseModalActionSet
  {

    [CommandPaletteOptOut("Running any palette row already closes the palette, the only modal open while it is shown.")]
    public class Action() : IBaseAction;

    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        ApplicationState.ActiveModalId = null;
        return default;
      }
    }
  }
}
