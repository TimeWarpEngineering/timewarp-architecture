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

    internal class Action() : IBaseAction;

    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ApplicationState.ActiveModalId = null;
        return Task.CompletedTask;
      }
    }
  }
}
