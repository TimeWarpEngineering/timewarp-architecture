#region Purpose
// Open: rebuild the palette roster for the current principal and reset the query.
#endregion

#region Design
// Authorization runs here (IAuthorizationService over the current AuthenticationState) rather
// than in the component so the permission filter is testable headless. Showing the overlay is
// the caller's next step (ApplicationState.SetActiveModal) — no nested dispatch.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class CommandPaletteState
{
  public static class OpenActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store,
      AuthenticationStateProvider authenticationStateProvider,
      IAuthorizationService authorizationService,
      IActionCatalog actionCatalog
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AuthenticationState authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        CommandPaletteState.Roster = await CommandPaletteRoster.BuildAsync
        (
          authenticationState.User,
          authorizationService,
          PageRegistry.All,
          actionCatalog.Entries
        );
        CommandPaletteState.Apply("");
      }
    }
  }
}
