namespace TimeWarp.Architecture.Features.Processing;

internal partial class ProcessingState
{
  public static class TwoSecondTask
  {
    [TrackProcessing]
    public record Action : BaseAction;

    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) { }

      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        Console.WriteLine("Start two Second Task");
        await Task.Delay(millisecondsDelay: 2000, cancellationToken: cancellationToken);
        Console.WriteLine("Two Second Task Complete");
      }
    }
  }
}
