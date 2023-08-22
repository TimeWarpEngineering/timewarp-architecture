namespace TimeWarp.Architecture.Features.Applications;

using static BlazorState.Features.Routing.RouteState;

internal partial class ApplicationState
{
  public static class ResetStore
  {

    internal record Action : BaseAction { }

    internal class Handler : IRequestHandler<Action>
    {
      private readonly ISender Sender;

      private readonly IStore Store;

      public Handler(IStore store, ISender sender)
      {
        Sender = sender;
        Store = store;
      }

      public async Task Handle(Action action, CancellationToken cancellationToken)
      {
        Store.Reset();
        await Sender.Send(new ChangeRouteAction { NewRoute = "/" }, cancellationToken).ConfigureAwait(false);
      }
    }
  }
}
