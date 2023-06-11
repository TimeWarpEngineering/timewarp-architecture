namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  internal record ToggleMenuAction : BaseAction { }
  internal class ToggleMenuHandler : BaseHandler<ToggleMenuAction>
  {
    public ToggleMenuHandler(IStore store) : base(store) { }

    public override Task Handle(ToggleMenuAction action, CancellationToken cancellationToken)
    {
      ApplicationState.IsMenuExpanded = !ApplicationState.IsMenuExpanded;
      return Task.CompletedTask;
    }
  }
}
