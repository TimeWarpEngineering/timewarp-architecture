namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public static class ResetStoreActionSet
  {
    internal class Action : IBaseAction;

    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) {}
      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        Store.Reset();
        await RouteState.ChangeRoute(newRoute: "/", cancellationToken);
      }
    }
  }
}
