namespace TimeWarp.Architecture.Features.Counters;

partial class CounterState
{
  // CA1711: Suppressed because 'ThrowException' is the name of an action container 
  // in our Flux-style state management (not an actual Exception type). 
  // Renaming it to avoid the suffix produces worse names (e.g. ThrowExceptionAction.Action).
  #pragma warning disable CA1711
  public static class ThrowException
  {
    public class Action(string Message) : IBaseAction
    {
      public string Message { get; init; } = Message;
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
      ) =>
        // Intentionally throw so we can test exception handling.
        throw new Exception(action.Message);
    }
  }
  #pragma warning restore CA1711
}
