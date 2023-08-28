namespace TimeWarp.Architecture.Features.Processing;

internal partial class ProcessingState
{
  public static class CompleteProcessing
  {
    internal record Action(string ActionName) : BaseAction;

    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore aStore) : base(aStore) { }

      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ProcessingState._ProcessingList.Remove(action.ActionName);
        return Task.CompletedTask;
      }
    }
  }
}
