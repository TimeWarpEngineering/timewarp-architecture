namespace TimeWarp.Architecture.Features.Counters;

internal partial class CounterState
{
  public static class IncrementCounter
  {
    public record Action(int Amount) : BaseAction;
    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore aStore) : base(aStore) { }

      public override Task Handle
      (
        Action action,
        CancellationToken aCancellationToken
      )
      {
        CounterState.Count += action.Amount;
        return Task.CompletedTask;
      }
    }
  }
}
