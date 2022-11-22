namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  internal class TwoSecondTaskHandler : BaseHandler<TwoSecondTaskAction>
  {
    public TwoSecondTaskHandler(IStore aStore) : base(aStore) { }

    public override async Task<Unit> Handle(TwoSecondTaskAction aTwoSecondTaskAction, CancellationToken aCancellationToken)
    {
      Console.WriteLine("Start 2 Second Task");
      await Task.Delay(millisecondsDelay: 2000, cancellationToken: aCancellationToken);
      Console.WriteLine("Completed 2 Second Task");
      return Unit.Value;
    }
  }
}
