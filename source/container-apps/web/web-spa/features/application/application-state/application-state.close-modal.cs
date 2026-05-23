namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{

  public static class CloseModalActionSet
  {

    internal class Action() : IBaseAction;


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
