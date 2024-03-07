namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  public static class SetActiveModal
  {
    internal record Action(string ModalId) : BaseAction;

    [UsedImplicitly]
    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ApplicationState.ActiveModalId = action.ModalId;
        return Task.CompletedTask;
      }
    }
  }
}
