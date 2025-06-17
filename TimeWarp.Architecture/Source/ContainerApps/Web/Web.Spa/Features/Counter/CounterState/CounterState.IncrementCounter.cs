namespace TimeWarp.Architecture.Features.Counters;

partial class CounterState
{
  public static class IncrementCounterActionSet
  {
    public class Action : IBaseAction
    {
      public int Amount { get; }
      public Action(int amount)
      {
        Amount = amount;
      }
    }

    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {

      public override Task Handle
      (
        Action action,
        CancellationToken cancellationToken
      )
      {
        CounterState.Count += action.Amount;
        return Task.CompletedTask;
      }
    }
  }
}
