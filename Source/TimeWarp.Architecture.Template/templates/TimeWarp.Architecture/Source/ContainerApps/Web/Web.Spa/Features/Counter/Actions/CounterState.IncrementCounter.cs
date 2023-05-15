namespace TimeWarp.Architecture.Features.Counters.Spa;

internal partial class CounterState
{
  public record IncrementCounterAction(int Amount) : BaseAction;
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
