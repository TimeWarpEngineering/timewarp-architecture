namespace TimeWarp.Blazor.Features.Counters.Client
{
  using BlazorState;
  using MediatR;
  using System.Threading;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases;

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
}
