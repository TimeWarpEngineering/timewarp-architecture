namespace TimeWarp.Architecture.Features.Counters;

using BlazorState;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.Bases;

internal partial class CounterState
{
  internal class IncrementCounterHandler : BaseHandler<IncrementCounterAction>
  {
    public IncrementCounterHandler(IStore aStore) : base(aStore) { }

    public override Task<Unit> Handle
    (
      IncrementCounterAction aIncrementCounterAction,
      CancellationToken aCancellationToken
    )
    {
      CounterState.Count += aIncrementCounterAction.Amount;
      return Unit.Task;
    }
  }
}
