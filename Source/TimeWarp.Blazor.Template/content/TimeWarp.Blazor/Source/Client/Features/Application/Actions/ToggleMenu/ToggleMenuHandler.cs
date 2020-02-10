namespace TimeWarp.Blazor.Features.Applications.Client
{
  using BlazorState;
  using MediatR;
  using System.Threading;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases.Client;

  internal partial class ApplicationState
  {
    internal class ToggleMenuHandler : BaseHandler<ToggleMenuAction>
    {
      public ToggleMenuHandler(IStore aStore) : base(aStore) { }

      public override Task<Unit> Handle(ToggleMenuAction aResetStoreAction, CancellationToken aCancellationToken)
      {
        ApplicationState.IsMenuExpanded = !ApplicationState.IsMenuExpanded;
        return Unit.Task;
      }
    }
  }
}
