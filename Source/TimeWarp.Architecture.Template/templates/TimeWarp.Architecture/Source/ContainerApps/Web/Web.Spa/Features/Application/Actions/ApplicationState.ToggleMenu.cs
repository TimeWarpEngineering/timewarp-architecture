namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  public static class ToggleMenu
  {
    internal record Action : BaseAction { }
    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) { }

      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ApplicationState.IsMenuExpanded = !ApplicationState.IsMenuExpanded;
        return Task.CompletedTask;
      }
    }
  }
}
