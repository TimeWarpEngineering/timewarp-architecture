namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  public static class StartProcessing
  {
    internal record Action(string ActionName) : BaseAction;

    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) { }

      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ApplicationState._ProcessingList.Add(action.ActionName);
        return Task.CompletedTask;
      }
    }
  }
}
