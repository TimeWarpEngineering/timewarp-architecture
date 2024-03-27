namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  public static class ToggleMenu
  {
    internal class Action : BaseAction { }

    [UsedImplicitly]
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
