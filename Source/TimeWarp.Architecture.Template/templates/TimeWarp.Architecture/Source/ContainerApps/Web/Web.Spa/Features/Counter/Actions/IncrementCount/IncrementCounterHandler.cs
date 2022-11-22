namespace TimeWarp.Architecture.Features.Counters;

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
