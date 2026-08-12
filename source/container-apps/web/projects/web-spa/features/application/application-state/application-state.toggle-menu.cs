#region Purpose
// ApplicationState action that flips the navigation menu between expanded and collapsed.
#endregion

#region Design
// Menu expansion lives in ApplicationState rather than in the nav component so any part of
// the shell can toggle or react to it; the default (expanded) is set in Initialize.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public static class ToggleMenu
  {
    internal class Action : IBaseAction;

    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {

      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ApplicationState.IsMenuExpanded = !ApplicationState.IsMenuExpanded;
        return Task.CompletedTask;
      }
    }
  }
}
