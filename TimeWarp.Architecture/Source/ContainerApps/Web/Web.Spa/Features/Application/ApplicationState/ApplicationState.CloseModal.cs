namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  [UsedImplicitly]
  public static class CloseModalActionSet
  {
    [UsedImplicitly]
    internal class Action() : IBaseAction;

    [UsedImplicitly]
    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ApplicationState.ActiveModalId = null;
        return Task.CompletedTask;
      }
    }
  }
}
