namespace TimeWarp.Architecture.Features.Counters.Spa;

internal partial class CounterState
{
  public record ThrowExceptionAction(string Message) : BaseAction;
  internal class ThrowExceptionHandler : BaseHandler<ThrowExceptionAction>
  {
    public ThrowExceptionHandler(IStore aStore) : base(aStore) { }

    public override Task Handle
    (
      ThrowExceptionAction aThrowExceptionAction,
      CancellationToken aCancellationToken
    ) =>
      // Intentionally throw so we can test exception handling.
      throw new Exception(aThrowExceptionAction.Message);
  }
}
