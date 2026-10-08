#region Purpose
// Chooses the catalog entries an agent may call on one page for one principal.
#endregion

#region Design
// Same gates for the in-app model and WebMCP. Human-only entries are never tools.
// The page list names the buttons on that route, in that order; visibility then drops the
// human ceremonies that share the page. Permissions use the palette's IAuthorizationService
// check (policy name == permission id). An anonymous principal gets nothing, matching Ctrl-K.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Page-scoped, permission-filtered catalog tools.</summary>
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
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(authorizationService);
    ArgumentNullException.ThrowIfNull(entries);
    cancellationToken.ThrowIfCancellationRequested();

    if (user.Identity?.IsAuthenticated != true)
    {
      return [];
    }

    IReadOnlyList<string> pageNames = PageAgentScope.ActionNamesFor(path);
    if (pageNames.Count == 0)
    {
      return [];
    }

    Dictionary<string, ActionCatalogEntry> byName = [];
    foreach (ActionCatalogEntry entry in entries)
    {
      byName[entry.Name] = entry;
    }

    List<CatalogAgentTool> tools = [];
    foreach (string name in pageNames)
    {
      if (!byName.TryGetValue(name, out ActionCatalogEntry? entry))
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

      string description = string.IsNullOrWhiteSpace(entry.Description)
        ? entry.DisplayName ?? entry.Name
        : entry.Description;
      tools.Add
      (
        new CatalogAgentTool
        (
          entry.Name,
          description,
          CatalogAgentSchema.For(entry),
          CatalogAgentApproval.RequiresApproval(entry),
          entry
        )
      );
    }

    return tools;
  }
}
