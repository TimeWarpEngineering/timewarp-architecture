namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  public static class ResetStore
  {

    internal class Action : IBaseAction {}

    [UsedImplicitly]
    internal class Handler : BaseHandler<Action>
    {
      private readonly ISender Sender;
      public Handler(IStore store, ISender sender) : base(store)
      {
        Sender = sender;
      }
      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        Store.Reset();
        await RouteState.ChangeRoute(newRoute: "/", cancellationToken);
      }
    }
  }
}
