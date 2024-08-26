namespace TimeWarp.Architecture.Features.Counters;

partial class CounterState
{
  public static class IncrementCounter
  {
    public class Action(int Amount) : IBaseAction
    {
      public int Amount { get; set; } = Amount;
    }

    [UsedImplicitly]
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
