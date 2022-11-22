namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  internal class ToggleMenuHandler : BaseHandler<ToggleMenuAction>
  {
    public ToggleMenuHandler(IStore aStore) : base(aStore) { }

    public override Task<Unit> Handle(ToggleMenuAction aResetStoreAction, CancellationToken aCancellationToken)
    {
      ApplicationState.IsMenuExpanded = !ApplicationState.IsMenuExpanded;
      return Unit.Task;
    }
  }
}
