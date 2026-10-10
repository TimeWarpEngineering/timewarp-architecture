#region Purpose
// Chooses the tools an agent may call on one page for one principal.
#endregion

#region Design
// The list is the Ctrl-K roster plus the current page's tools. CommandPaletteRoster stays the
// filter for pages and commands; this type only reads it, so the palette rows do not change.
// A palette command becomes a tool when its visibility includes Agent. Human-only commands stay
// in the palette and never become tools. Page-bound commands (PageAgentScope) are executable on
// their page and, off that page, are still listed: invoking one returns a navigate offer and does
// not run. Actions that are not palette commands stay executable only on their page.
// navigate is one tool. Its enum is the palette's Page rows for this principal, including Sign in
// while signed out. It is not an edit, so RequiresApproval is false in both edit modes.
// FindOfferedAsync re-checks permission for the principal at call time. A global tool survives
// navigation. A page-only tool does not. A page-bound palette tool survives as a discovery offer.
// Edit mode changes only the approval bit of tools that will actually run. The conversation
// credential is not a selection filter; both drivers enforce it at invoke time.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Palette tools, navigate, and the current page's tools.</summary>
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
    bool anyPage = false;
    foreach (CommandPaletteRow row in rows)
    {
      if (row.Kind == CommandPaletteRowKind.Page)
      {
        anyPage = true;
        continue;
      }

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

      string? offPage = PageAgentScope.Serves(normalized, entry.Name)
        ? null
        : PageAgentScope.PrimaryPage(entry.Name);
      tools.Add(ToTool(entry, editMode, offPage));
    }

    foreach (string name in PageAgentScope.ActionNamesFor(normalized))
    {
      if (!seen.Add(name) || !byName.TryGetValue(name, out ActionCatalogEntry? entry))
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

    if (anyPage)
    {
      List<CommandPaletteRow> pages = [];
      foreach (CommandPaletteRow row in rows)
      {
        if (row.Kind == CommandPaletteRowKind.Page)
        {
          pages.Add(row);
        }
      }

      tools.Add(AgentNavigate.Create(pages));
    }

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
