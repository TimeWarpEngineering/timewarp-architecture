namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  public static class FiveSecondTask
  {

    [TrackProcessing]
    internal record Action : BaseAction { }

    internal record struct CompleteNotification : INotification;

    internal class Handler : BaseHandler<Action>
    {
      private readonly IPublisher Publisher;

      public Handler(IStore aStore, IPublisher publisher) : base(aStore)
      {
        Publisher = publisher;
      }

      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        Console.WriteLine("Start");
        await Task.Delay(millisecondsDelay: 5000, cancellationToken: cancellationToken);
        await Publisher.Publish(new CompleteNotification(), cancellationToken);
        Console.WriteLine("Done");
      }
    }
  }
}
