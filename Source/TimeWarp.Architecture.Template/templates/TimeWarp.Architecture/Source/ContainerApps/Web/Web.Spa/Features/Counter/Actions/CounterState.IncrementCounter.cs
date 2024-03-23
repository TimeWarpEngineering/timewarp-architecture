namespace TimeWarp.Architecture.Features.Counters;

internal partial class CounterState
{
  public static class IncrementCounter
  {
    public record Action(int Amount) : BaseAction;

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
