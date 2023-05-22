namespace TimeWarp.Architecture.Features.Applications.Spa;

internal partial class ApplicationState
{
  internal record StartProcessingAction(string ActionName) : BaseAction;
  
  internal class StartProcessingHandler : BaseHandler<StartProcessingAction>
  {
    public StartProcessingHandler(IStore aStore) : base(aStore) { }

    public override Task Handle(StartProcessingAction aStartWaitingAction, CancellationToken aCancellationToken)
    {
      ApplicationState._ProcessingList.Add(aStartWaitingAction.ActionName);
      return Task.CompletedTask;
    }
  }
}
