namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  internal record SetActiveModalAction(string ModalId) : BaseAction;

  internal class SetActiveModalHandler : BaseHandler<SetActiveModalAction>
  {
    public SetActiveModalHandler(IStore aStore) : base(aStore) { }

    public override Task<Unit> Handle
    (
      SetActiveModalAction aSetActiveModalAction,
      CancellationToken aCancellationToken
    )
    {
      ApplicationState.ActiveModalId = aSetActiveModalAction.ModalId;
      return Unit.Task;
    }
  }
}
