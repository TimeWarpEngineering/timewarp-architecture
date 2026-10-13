#region Purpose
// SPA state for the admin principals list (search, sort), the open principal, and role assignment.
#endregion

#region Design
// Principals null = no snapshot yet; empty = loaded with zero rows.
// In-flight fetch is [TrackAction] on FetchPrincipals — pages use IsAnyActive, not null.
// DraftRoleIds tracks multi-select edits per principal before Save → SetPrincipalRoles.
// LastSetPrincipalRolesSucceeded is the page-side Fetch gate: DefaultApiHandler does not
// throw on problem details, so OnSave Fetchs only when HandleSuccess ran.
// Task 328: Search, SortBy, and SortDescending are State actions (SetSearch, SetSort); the page
// dispatches FetchPrincipals afterwards and the server filters and sorts the whole list. A failed
// list fetch clears the rows and sets ListLoadError so a stale list never shows under a new
// query. Current is the open principal (OpenPrincipal); a 404 clears it and sets LoadError.
// Role drafts are seeded from the opened principal, not the list: roles are edited on the
// detail page, so a list re-fetch never overwrites a draft. Initialize clears everything.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using static ListPrincipals;

[StateAccess]
public sealed partial class PrincipalState : State<PrincipalState>
{
  private List<PrincipalSummaryDto>? PrincipalsList { get; set; }
  private Dictionary<Guid, HashSet<Guid>> DraftRoleIds { get; set; } = [];

  public IReadOnlyList<PrincipalSummaryDto>? Principals => PrincipalsList?.AsReadOnly();

  public bool LastSetPrincipalRolesSucceeded { get; private set; }
  public string? Search { get; private set; }
  public PrincipalListSortColumn SortBy { get; private set; } = PrincipalListSortColumn.CreatedAt;
  public bool SortDescending { get; private set; }
  public GetPrincipal.PrincipalDetailDto? Current { get; private set; }
  public string? LoadError { get; private set; }
  public string? ListLoadError { get; private set; }

  public override void Initialize()
  {
    PrincipalsList = null;
    DraftRoleIds = [];
    LastSetPrincipalRolesSucceeded = false;
    Search = null;
    SortBy = PrincipalListSortColumn.CreatedAt;
    SortDescending = false;
    Current = null;
    LoadError = null;
    ListLoadError = null;
  }

  public IReadOnlyCollection<Guid> GetDraftRoleIds(Guid principalId) =>
    DraftRoleIds.TryGetValue(principalId, out HashSet<Guid>? set)
      ? set
      : Array.Empty<Guid>();

  public bool IsRoleSelected(Guid principalId, Guid roleId) =>
    DraftRoleIds.TryGetValue(principalId, out HashSet<Guid>? set) && set.Contains(roleId);
}
