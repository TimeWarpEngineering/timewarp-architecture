namespace TimeWarp.Architecture.Features.Applications
{
  using BlazorState;
  using MediatR;
  using System.Threading;
  using System.Threading.Tasks;
  using TimeWarp.Architecture.Features.Bases;

  internal partial class ApplicationState
  {
    internal class StartProcessingHandler : BaseHandler<StartProcessingAction>
    {
      public StartProcessingHandler(IStore aStore) : base(aStore) { }

      public override Task<Unit> Handle(StartProcessingAction aStartWaitingAction, CancellationToken aCancellationToken)
      {
        ApplicationState._ProcessingList.Add(aStartWaitingAction.ActionName);
        return Unit.Task;
      }
    }
  }
}
