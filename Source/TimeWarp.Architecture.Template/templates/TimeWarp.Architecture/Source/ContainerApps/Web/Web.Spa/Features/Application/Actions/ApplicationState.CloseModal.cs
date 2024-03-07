namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  [UsedImplicitly]
  public static class CloseModal
  {
    [UsedImplicitly]
    internal record Action() : BaseAction;

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
