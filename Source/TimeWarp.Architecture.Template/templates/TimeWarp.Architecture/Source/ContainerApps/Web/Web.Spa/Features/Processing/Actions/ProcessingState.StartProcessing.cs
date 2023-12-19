namespace TimeWarp.Architecture.Features.Processing;

internal partial class ProcessingState
{
  public static class StartProcessing
  {
    internal record Action(string ActionName) : BaseAction;

    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) { }

      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ProcessingState._ProcessingList.Add(action.ActionName);
        return Task.CompletedTask;
      }
    }
  }
}
