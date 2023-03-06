namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  internal class FiveSecondTaskHandler : BaseHandler<FiveSecondTaskAction>
  {
    public FiveSecondTaskHandler(IStore aStore) : base(aStore) { }

    public override async Task Handle(FiveSecondTaskAction aFiveSecondTaskAction, CancellationToken aCancellationToken)
    {
      Console.WriteLine("Start");
      await Task.Delay(millisecondsDelay: 5000, cancellationToken: aCancellationToken);
      Console.WriteLine("Done");
    }
  }
}
