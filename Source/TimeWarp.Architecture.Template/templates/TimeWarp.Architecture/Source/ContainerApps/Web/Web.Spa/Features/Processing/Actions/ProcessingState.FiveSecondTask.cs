namespace TimeWarp.Architecture.Features.Processing;

internal partial class ProcessingState
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
        Console.WriteLine("Start Five Second Task");
        await Task.Delay(millisecondsDelay: 5000, cancellationToken: cancellationToken);
        await Publisher.Publish(new CompleteNotification(), cancellationToken);
        Console.WriteLine("Five Second Task Complete");
      }
    }
  }
}
