namespace TimeWarp.Blazor.Client.Features.Application
{
  using System.Threading;
  using System.Threading.Tasks;
  using BlazorState;
  using TimeWarp.Blazor.Client.Features.Base;

  internal partial class ApplicationState
  {
    internal class ToggleMenuHandler : BaseHandler<ToggleMenuAction, ApplicationState>
    {
      public ToggleMenuHandler(IStore aStore) : base(aStore) { }

      public override Task<ApplicationState> Handle(ToggleMenuAction aResetStoreAction, CancellationToken aCancellationToken)
      {
        ApplicationState.IsMenuExpanded = !ApplicationState.IsMenuExpanded;
        return Task.FromResult(ApplicationState);
      }
    }
  }
}
