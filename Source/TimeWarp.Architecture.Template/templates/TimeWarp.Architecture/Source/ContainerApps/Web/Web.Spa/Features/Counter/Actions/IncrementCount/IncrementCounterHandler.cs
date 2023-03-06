namespace TimeWarp.Architecture.Features.Counters;

internal partial class CounterState
{
  internal class IncrementCounterHandler : BaseHandler<IncrementCounterAction>
  {
    public IncrementCounterHandler(IStore aStore) : base(aStore) { }

    public override Task Handle
    (
      IncrementCounterAction aIncrementCounterAction,
      CancellationToken aCancellationToken
    )
    {
      CounterState.Count += aIncrementCounterAction.Amount;
      return Task.CompletedTask;
    }
  }
}
