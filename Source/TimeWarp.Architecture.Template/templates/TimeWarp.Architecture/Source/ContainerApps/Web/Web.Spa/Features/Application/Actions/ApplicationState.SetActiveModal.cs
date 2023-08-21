namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  public static class SetActiveModal
  {

    internal record Action(string ModalId) : BaseAction;

    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) { }

      public override Task<Unit> Handle(Action action, CancellationToken cancellationToken)
      {
        ApplicationState.ActiveModalId = action.ModalId;
        return Unit.Task;
      }
    }
  }
}
