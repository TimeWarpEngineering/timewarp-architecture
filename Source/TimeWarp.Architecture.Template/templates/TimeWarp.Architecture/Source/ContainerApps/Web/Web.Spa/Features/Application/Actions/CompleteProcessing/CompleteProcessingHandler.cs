namespace TimeWarp.Architecture.Features.Applications;

using BlazorState;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.Bases;

internal partial class ApplicationState
{
  internal class CompleteProcessingHandler : BaseHandler<CompleteProcessingAction>
  {
    public CompleteProcessingHandler(IStore aStore) : base(aStore) { }

    public override Task<Unit> Handle(CompleteProcessingAction aStopWaitingAction, CancellationToken aCancellationToken)
    {
      ApplicationState._ProcessingList.Remove(aStopWaitingAction.ActionName);
      return Unit.Task;
    }
  }
}
