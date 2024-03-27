namespace TimeWarp.Architecture.Features.Sidebars;

internal partial class SidebarState
{
  public static class OpenSideBar
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
        SidebarState.IsOpen = true;
        return Task.CompletedTask;
      }
    }
  }
}
