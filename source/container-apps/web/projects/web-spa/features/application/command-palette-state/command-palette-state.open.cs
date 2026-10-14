#region Purpose
// Open: rebuild the palette roster for the current principal and reset the query.
#endregion

#region Design
// Authorization runs here (IAuthorizationService over the current AuthenticationState) rather
// than in the component so the permission filter is testable headless. Showing the overlay is
// the caller's next step (ApplicationState.SetActiveModal) — no nested dispatch. The current
// base-relative path is read here too, so the signed-out Sign in row can return the visitor to it.
// Commands are the generated ActionRoster (task 301); IActionCatalog is optional metadata and the
// registered IActionDisplayOverride services supply run-time labels.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class CommandPaletteState
{
  public static class OpenActionSet
  {
    [CommandPaletteOptOut("Opens the palette itself; Ctrl-K and the app-bar search field own it.")]
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store,
      AuthenticationStateProvider authenticationStateProvider,
      IAuthorizationService authorizationService,
      IActionCatalog actionCatalog,
      IEnumerable<IActionDisplayOverride> displayOverrides,
      NavigationManager navigationManager
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        // One read for this dispatch. The roster is rebuilt on the next Open.
#pragma warning disable BL0013
        AuthenticationState authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
#pragma warning restore BL0013
        CommandPaletteState.Roster = await CommandPaletteRoster.BuildAsync
        (
          authenticationState.User,
          authorizationService,
          PageRegistry.All,
          ActionRoster.All,
          actionCatalog.Entries,
          displayOverrides,
          "/" + navigationManager.ToBaseRelativePath(navigationManager.Uri)
        );
        CommandPaletteState.Apply("");
      }
    }
  }
}
