#region Purpose
// The current page's contextual Ctrl-K rows, gathered from every ICommandPaletteContextSource for the current path.
#endregion

#region Design
// Scoped service (one per circuit/tab, like the store). Rows() is what CommandPaletteState.Open adds
// to the roster; IsOffered() is the runner's fail-closed gate — a contextual row runs only if the
// current page still contributes exactly that row (target, arguments and follow-up included), so a
// row from an older payload, another page, or a hand-built row is refused. The path is the
// base-relative path without query or fragment, with a leading "/".
// RefusalAsync is the second gate (task 279, closes 275's review M4): before the runner executes a
// contextual (server-offered) row, the catalog entry it names must be human-visible (Visibility Human
// or Both) and the signed-in principal must pass every one of its Permissions through
// IAuthorizationService — the same metadata and policies the static roster uses
// (CommandPaletteRoster.IsPermittedAsync). Chosen over an explicit offerable-names list: the catalog
// already declares who may run each action, so a second list would be one more thing to keep in
// agreement, and an agent surface (task 271) can apply the same check from the same metadata. The
// catalog stays the outer allow-list (an unknown name never resolves); this narrows it to actions a
// human here could have run anyway, so a server offer can never reach an Agent-only or unpermitted
// entry. The server still re-enforces on the real endpoint.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

public sealed class CommandPaletteContext
{
  private readonly IEnumerable<ICommandPaletteContextSource> Sources;
  private readonly NavigationManager NavigationManager;
  private readonly AuthenticationStateProvider AuthenticationStateProvider;
  private readonly IAuthorizationService AuthorizationService;

  public CommandPaletteContext
  (
    IEnumerable<ICommandPaletteContextSource> sources,
    NavigationManager navigationManager,
    AuthenticationStateProvider authenticationStateProvider,
    IAuthorizationService authorizationService
  )
  {
    Sources = sources;
    NavigationManager = navigationManager;
    AuthenticationStateProvider = authenticationStateProvider;
    AuthorizationService = authorizationService;
  }

  /// <summary>The current base-relative path, e.g. "/Settings".</summary>
  public string CurrentPath
  {
    get
    {
      string relative = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
      int cut = relative.IndexOfAny(['?', '#']);
      return "/" + (cut < 0 ? relative : relative[..cut]);
    }
  }

  /// <summary>Every contextual row the current page contributes now.</summary>
  public IReadOnlyList<CommandPaletteRow> Rows()
  {
    string path = CurrentPath;
    return [.. Sources.SelectMany(source => source.GetRows(path))];
  }

  /// <summary>True when the current page still contributes exactly <paramref name="row"/>.</summary>
  public bool IsOffered(CommandPaletteRow row) =>
    row.Kind == CommandPaletteRowKind.Contextual && Rows().Contains(row);

  /// <summary>Null when the current principal may run <paramref name="entry"/> as a contextual action; otherwise why not.</summary>
  public async Task<string?> RefusalAsync(ActionCatalogEntry entry)
  {
    if (entry.Visibility is not (ActionVisibility.Human or ActionVisibility.Both))
    {
      return $"'{entry.Name}' is not an action people run here";
    }

    ClaimsPrincipal user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
    if (user.Identity?.IsAuthenticated != true)
    {
      return "sign in first";
    }

    return await CommandPaletteRoster.IsPermittedAsync(user, AuthorizationService, entry)
      ? null
      : $"you are not permitted to run '{entry.Name}'";
  }
}
