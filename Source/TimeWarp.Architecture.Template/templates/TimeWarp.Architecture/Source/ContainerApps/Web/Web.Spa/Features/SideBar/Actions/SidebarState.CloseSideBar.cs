namespace TimeWarp.Architecture.Features.Sidebars;

internal partial class SidebarState
{
  public static class CloseSideBar
  {
    internal record Action : BaseAction { }
    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) { }

      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        SidebarState.IsOpen = false;
        return Task.CompletedTask;
      }
    }
  }
}
