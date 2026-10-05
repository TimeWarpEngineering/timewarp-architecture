#region Purpose
// Open: rebuild the palette roster for the current principal and reset the query.
#endregion

#region Design
// Authorization runs here (IAuthorizationService over the current AuthenticationState) rather
// than in the component so the permission filter is testable headless. Showing the overlay is
// the caller's next step (ApplicationState.SetActiveModal) — no nested dispatch. The current
// base-relative path is read here too, so the signed-out Sign in row can return the visitor to it.
// Contextual rows (task 275) are appended from CommandPaletteContext — whatever the current page
// contributes right now, minus rows that need input the palette cannot collect; every other page
// contributes none, so their roster is unchanged.
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
      IActionCatalog actionCatalog,
      NavigationManager navigationManager,
      CommandPaletteContext commandPaletteContext
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AuthenticationState authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        IReadOnlyList<CommandPaletteRow> roster = await CommandPaletteRoster.BuildAsync
        (
          authenticationState.User,
          authorizationService,
          PageRegistry.All,
          actionCatalog.Entries,
          "/" + navigationManager.ToBaseRelativePath(navigationManager.Uri)
        );
        CommandPaletteState.Roster = [.. commandPaletteContext.Rows().Where(static row => !row.RequiresInput), .. roster];
        CommandPaletteState.Apply("");
      }
    }
  }
}
