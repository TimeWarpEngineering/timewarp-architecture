namespace TimeWarp.Architecture.Features.Counters;

partial class CounterState
{
  public static class ThrowException
  {
    public class Action(string Message) : IBaseAction
    {
      public string Message { get; init; } = Message;
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
        CancellationToken aCancellationToken
      ) =>
        // Intentionally throw so we can test exception handling.
        throw new Exception(action.Message);
    }
  }
}
