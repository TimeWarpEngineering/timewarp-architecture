namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  internal record ToggleMenuAction : BaseAction { }
  internal class ToggleMenuHandler : BaseHandler<ToggleMenuAction>
  {
    public ToggleMenuHandler(IStore aStore) : base(aStore) { }

    public override Task Handle(ToggleMenuAction aResetStoreAction, CancellationToken aCancellationToken)
    {
      ApplicationState.IsMenuExpanded = !ApplicationState.IsMenuExpanded;
      return Task.CompletedTask;
    }
  }
}
