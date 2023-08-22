namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  public static class CompleteProcessing
  {
    internal record Action(string ActionName) : BaseAction;

    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore aStore) : base(aStore) { }

      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ApplicationState._ProcessingList.Remove(action.ActionName);
        return Task.CompletedTask;
      }
    }
  }
}
