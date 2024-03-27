namespace TimeWarp.Architecture.Features.Applications;

using static TimeWarp.Features.Routing.RouteState;

internal partial class ApplicationState
{
  public static class ResetStore
  {

    internal class Action : BaseAction { }

    [UsedImplicitly]
    internal class Handler
    (
      IStore Store,
      ISender Sender
    ) : IRequestHandler<Action>
    {
      public async Task Handle(Action action, CancellationToken cancellationToken)
      {
        Store.Reset();
        await Sender.Send(new ChangeRoute.Action { NewRoute = "/" }, cancellationToken).ConfigureAwait(false);
      }
    }
  }
}
