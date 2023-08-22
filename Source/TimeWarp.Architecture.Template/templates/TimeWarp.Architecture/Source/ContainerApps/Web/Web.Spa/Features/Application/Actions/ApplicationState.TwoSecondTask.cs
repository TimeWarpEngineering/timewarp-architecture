namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
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
        Console.WriteLine("Start 2 Second Task");
        await Task.Delay(millisecondsDelay: 2000, cancellationToken: cancellationToken);
        Console.WriteLine("Completed 2 Second Task");
      }
    }
  }
}
