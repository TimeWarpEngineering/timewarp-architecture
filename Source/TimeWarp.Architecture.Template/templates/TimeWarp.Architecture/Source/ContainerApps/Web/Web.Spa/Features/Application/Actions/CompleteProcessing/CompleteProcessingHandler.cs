namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  internal class CompleteProcessingHandler : BaseHandler<CompleteProcessingAction>
  {
    public CompleteProcessingHandler(IStore aStore) : base(aStore) { }

    public override Task Handle(CompleteProcessingAction aStopWaitingAction, CancellationToken aCancellationToken)
    {
      ApplicationState._ProcessingList.Remove(aStopWaitingAction.ActionName);
      return Task.CompletedTask;
    }
  }
}
