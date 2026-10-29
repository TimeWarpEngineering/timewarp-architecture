#region Purpose
// Chooses the tools an agent may call on one page for one principal.
#endregion

#region Design
// The list is the Ctrl-K commands, the current page's own actions, navigate, and site_map.
// CommandPaletteRoster stays the filter for commands; this type only reads it.
// A palette command becomes a tool when its visibility includes Agent. Human-only commands stay
// in the palette and never become tools. Page-bound commands (PageAgentScope, derived from
// [Page(AgentActions = ...)]) are executable on their page and, off that page, are still listed:
// invoking one returns a navigate offer and does not run. A page-bound command whose pages all
// have parameterized routes has no page to offer, so it is not listed off them. Actions that are
// not palette commands stay executable only on their page, and only for a signed-in principal,
// matching the palette's sign-in rule for commands, so an anonymous visitor gets navigate and
// site_map alone.
// navigate and site_map share AgentSiteMap.DestinationsAsync, the menu's destinations for this
// principal (Sign in included while signed out, Ctrl-K's deep links last), so both answer and move across the whole app
// from every page, including Home (task 300). site_map is read-only and never needs approval.
// navigate needs approval in Ask before editing and runs at once in Automatically edit.
// FindOfferedAsync re-checks permission for the principal at call time. A global tool survives
// navigation. A page-only tool does not. A page-bound palette tool survives as a discovery offer.
// Edit mode changes only the approval bit of tools that will actually run. The conversation
// credential is not a selection filter; both drivers enforce it at invoke time.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Palette tools, the current page's tools, navigate, and site_map.</summary>
public static class CatalogAgentToolSet
{
  public static async Task<IReadOnlyList<CatalogAgentTool>> SelectAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IEnumerable<ActionCatalogEntry> entries,
    string? path,
    CancellationToken cancellationToken
  )
  {
    return await SelectAsync(
      user,
      authorizationService,
      entries,
      path,
      AgentEditMode.AskBeforeEditing,
      cancellationToken);
  }

  public static async Task<IReadOnlyList<CatalogAgentTool>> SelectAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IEnumerable<ActionCatalogEntry> entries,
    string? path,
    AgentEditMode editMode,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(authorizationService);
    ArgumentNullException.ThrowIfNull(entries);
    cancellationToken.ThrowIfCancellationRequested();

    string normalized = PageAgentScope.Normalize(path);
    List<ActionCatalogEntry> entryList = [.. entries];
    IReadOnlyList<CommandPaletteRow> rows = await CommandPaletteRoster.BuildAsync
    (
      user,
      authorizationService,
      PageRegistry.All,
      entryList,
      normalized
    );

    Dictionary<string, ActionCatalogEntry> byName = [];
    foreach (ActionCatalogEntry entry in entryList)
    {
      byName[entry.Name] = entry;
    }

    List<CatalogAgentTool> tools = [];
    HashSet<string> seen = [];
    foreach (CommandPaletteRow row in rows)
    {
      if (row.Kind != CommandPaletteRowKind.Command)
      {
        continue;
      }

      if (!byName.TryGetValue(row.Target, out ActionCatalogEntry? entry))
      {
        continue;
      }

      if (!entry.Visibility.HasFlag(ActionVisibility.Agent) || !seen.Add(entry.Name))
      {
        continue;
      }

      string? offPage = null;
      if (!PageAgentScope.Serves(normalized, entry.ActionType))
      {
        offPage = PageAgentScope.PrimaryPage(entry.ActionType);
        if (offPage is null)
        {
          continue;
        }
      }

      tools.Add(ToTool(entry, editMode, offPage));
    }

    IReadOnlyList<Type> pageActions = user.Identity?.IsAuthenticated == true
      ? PageAgentScope.ActionTypesFor(normalized)
      : [];
    foreach (Type actionType in pageActions)
    {
      ActionCatalogEntry? entry = entryList.Find(candidate => candidate.ActionType == actionType);
      if (entry is null || !seen.Add(entry.Name))
      {
        continue;
      }

      if (!entry.Visibility.HasFlag(ActionVisibility.Agent))
      {
        continue;
      }

      if (!await CommandPaletteRoster.IsPermittedAsync(user, authorizationService, entry))
      {
        continue;
      }

      tools.Add(ToTool(entry, editMode, offPageRoute: null));
    }

    IReadOnlyList<AgentSiteMap.Destination> destinations =
      await AgentSiteMap.DestinationsAsync(user, authorizationService, normalized);
    if (destinations.Count > 0)
    {
      tools.Add(AgentNavigate.Create(destinations, editMode));
    }

    tools.Add(AgentSiteMap.Create());
    return tools;
  }

  /// <summary>The named tool if it is offered now, for this principal on this path; otherwise null.</summary>
  public static async Task<CatalogAgentTool?> FindOfferedAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IEnumerable<ActionCatalogEntry> entries,
    string? path,
    string name,
    CancellationToken cancellationToken
  )
  {
    return await FindOfferedAsync(
      user,
      authorizationService,
      entries,
      path,
      name,
      AgentEditMode.AskBeforeEditing,
      cancellationToken);
  }

  /// <summary>The named tool if it is offered now, for this principal on this path; otherwise null.</summary>
  public static async Task<CatalogAgentTool?> FindOfferedAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IEnumerable<ActionCatalogEntry> entries,
    string? path,
    string name,
    AgentEditMode editMode,
    CancellationToken cancellationToken
  )
  {
    IReadOnlyList<CatalogAgentTool> tools = await SelectAsync
    (
      user,
      authorizationService,
      entries,
      path,
      editMode,
      cancellationToken
    );
    foreach (CatalogAgentTool tool in tools)
    {
      if (string.Equals(tool.Name, name, StringComparison.Ordinal))
      {
        return tool;
      }
    }

    return null;
  }

  private static CatalogAgentTool ToTool(ActionCatalogEntry entry, AgentEditMode editMode, string? offPageRoute)
  {
    string description = string.IsNullOrWhiteSpace(entry.Description)
      ? entry.DisplayName ?? entry.Name
      : entry.Description;
    if (offPageRoute is not null)
    {
      description += " It runs only on " + offPageRoute
        + ". From any other page it does not run and returns a navigate offer.";
    }

    bool approval = offPageRoute is null && CatalogAgentApproval.RequiresApproval(entry, editMode);
    return new CatalogAgentTool
    (
      entry.Name,
      description,
      CatalogAgentSchema.For(entry),
      approval,
      entry,
      offPageRoute
    );
  }
}
