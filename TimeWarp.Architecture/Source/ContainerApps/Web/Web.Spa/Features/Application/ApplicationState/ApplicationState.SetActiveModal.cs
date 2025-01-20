namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public static class SetActiveModalActionSet
  {
    internal class Action : IBaseAction
    {
      public string ModalId { get; }
      public Action(string modalId)
      {
        ModalId = modalId;
      }
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
