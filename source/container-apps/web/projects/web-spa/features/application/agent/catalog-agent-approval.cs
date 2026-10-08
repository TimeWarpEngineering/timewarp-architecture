#region Purpose
// Decides which catalog tools require a person to approve before they run.
#endregion

#region Design
// timewarp-state has no read-only flag. Until it does, a tool is read-only when the action
// segment (the catalog name after the last dot) starts with Fetch, Get, List, or Search.
// Everything else is mutating and must be wrapped for approval. Permissions are a separate
// gate: an action the principal cannot run is not offered, and approval does not grant it.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

public static class CatalogAgentApproval
{
  public static bool RequiresApproval(ActionCatalogEntry entry)
  {
    ArgumentNullException.ThrowIfNull(entry);
    int dot = entry.Name.LastIndexOf('.', StringComparison.Ordinal);
    string action = dot < 0 ? entry.Name : entry.Name[(dot + 1)..];
    return !IsReadOnlyName(action);
  }

  private static bool IsReadOnlyName(string action) =>
    action.StartsWith("Fetch", StringComparison.Ordinal)
    || action.StartsWith("Get", StringComparison.Ordinal)
    || action.StartsWith("List", StringComparison.Ordinal)
    || action.StartsWith("Search", StringComparison.Ordinal);
}
