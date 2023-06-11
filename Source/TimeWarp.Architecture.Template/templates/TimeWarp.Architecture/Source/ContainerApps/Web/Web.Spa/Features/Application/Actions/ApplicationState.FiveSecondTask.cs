namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  [TrackProcessing]
  internal record FiveSecondTaskAction : BaseAction { }

  internal record struct FiveSecondTaskCompleteNotification : INotification;

  internal class FiveSecondTaskHandler : BaseHandler<FiveSecondTaskAction>
  {
    private readonly IPublisher Publisher;

    public FiveSecondTaskHandler(IStore aStore, IPublisher publisher) : base(aStore)
    {
      Publisher = publisher;
    }

    public override async Task Handle(FiveSecondTaskAction aFiveSecondTaskAction, CancellationToken aCancellationToken)
    {
      Console.WriteLine("Start");
      await Task.Delay(millisecondsDelay: 5000, cancellationToken: aCancellationToken);
      await Publisher.Publish(new FiveSecondTaskCompleteNotification(), aCancellationToken);
      Console.WriteLine("Done");
    }
  }
}

