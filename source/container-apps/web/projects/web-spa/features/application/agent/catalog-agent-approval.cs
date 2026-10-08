#region Purpose
// Decides which catalog tools require a person to approve before they run.
#endregion

#region Design
// timewarp-state has no read-only flag. Until it does, read-only is an explicit allow-list of
// catalog names; every other tool is mutating and must be wrapped for approval. A name rule
// (Fetch/Get/List/Search prefixes) was rejected: a mutating action that happened to use one of
// those prefixes would silently skip approval. Adding an entry here is a reviewed decision that
// the action changes no state the person cares about. Permissions are a separate gate: an
// action the principal cannot run is not offered, and approval does not grant it.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

public static class CatalogAgentApproval
{
  private static readonly HashSet<string> ReadOnlyActions =
  [
    "Credentials.FetchCredentials",
  ];

  public static bool RequiresApproval(ActionCatalogEntry entry)
  {
    ArgumentNullException.ThrowIfNull(entry);
    return !ReadOnlyActions.Contains(entry.Name);
  }
}
