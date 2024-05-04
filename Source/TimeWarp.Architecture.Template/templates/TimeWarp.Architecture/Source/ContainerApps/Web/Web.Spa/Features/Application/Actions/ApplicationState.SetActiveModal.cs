namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  public static class SetActiveModal
  {
    internal class Action(string ModalId) : IBaseAction
    {
      public string ModalId { get; set; } = ModalId;
    }

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
