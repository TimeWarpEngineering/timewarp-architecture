namespace TimeWarp.Architecture.Features.Applications;

using static BlazorState.Features.Routing.RouteState;

internal partial class ApplicationState
{
  internal record ResetStoreAction : BaseAction { }

  internal class ResetStoreHandler : IRequestHandler<ResetStoreAction>
  {
    private readonly ISender Sender;

    private readonly IStore Store;

    public ResetStoreHandler(IStore aStore, ISender aSender)
    {
      Sender = aSender;
      Store = aStore;
    }

    public async Task Handle(ResetStoreAction aResetStoreAction, CancellationToken aCancellationToken)
    {
      Store.Reset();
      await Sender.Send(new ChangeRouteAction { NewRoute = "/" }, aCancellationToken).ConfigureAwait(false);
    }
  }
}
