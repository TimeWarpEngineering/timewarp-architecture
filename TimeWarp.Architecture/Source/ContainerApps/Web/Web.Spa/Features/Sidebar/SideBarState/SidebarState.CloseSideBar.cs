namespace TimeWarp.Architecture.Features.Sidebars;

partial class SidebarState
{
  public static class CloseSideBarActionSet
  {
    internal class Action : IBaseAction { }

    [UsedImplicitly]
    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        SidebarState.IsOpen = false;
        return Task.CompletedTask;
      }
    }
  }
}
