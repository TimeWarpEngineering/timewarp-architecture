namespace TimeWarp.Architecture.Features.Counters;

internal partial class CounterState
{
  public static class ThrowException
  {
    public record Action(string Message) : BaseAction;
    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) { }

      public override Task Handle
      (
        Action action,
        CancellationToken aCancellationToken
      ) =>
        // Intentionally throw so we can test exception handling.
        throw new Exception(action.Message);
    }
  }
}
