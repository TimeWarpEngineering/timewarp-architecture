#region Purpose
// SPA state for the admin principals list (search, sort) and the open principal.
#endregion

#region Design
// Principals null = no snapshot yet; empty = loaded with zero rows.
// In-flight fetch is [TrackAction] on FetchPrincipals — pages use IsAnyActive, not null.
// LastSetPrincipalRolesSucceeded is the page-side Fetch gate: DefaultApiHandler does not
// throw on problem details, so OnSave reopens only when HandleSuccess ran.
// Task 328: Search, SortBy, and SortDescending are State actions (SetSearch, SetSort); the page
// dispatches FetchPrincipals afterwards and the server filters and sorts the whole list. A failed
// list fetch clears the rows and sets ListLoadError so a stale list never shows under a new
// query. Current is the open principal (OpenPrincipal); a 404 clears it and sets LoadError.
// Role and agent-scope checkbox drafts are not stored here. PrincipalDetailPage owns those
// sets and builds the save commands from them. Initialize clears everything.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using static ListPrincipals;

[StateAccess]
public sealed partial class PrincipalState : State<PrincipalState>
{
  private List<PrincipalSummaryDto>? PrincipalsList { get; set; }

  public IReadOnlyList<PrincipalSummaryDto>? Principals => PrincipalsList?.AsReadOnly();

  public bool LastSetPrincipalRolesSucceeded { get; private set; }
  public bool LastSetPrincipalAgentScopesSucceeded { get; private set; }
  public string? Search { get; private set; }
  public PrincipalListSortColumn SortBy { get; private set; } = PrincipalListSortColumn.CreatedAt;
  public bool SortDescending { get; private set; }
  public GetPrincipal.PrincipalDetailDto? Current { get; private set; }
  public string? LoadError { get; private set; }
  public string? ListLoadError { get; private set; }

  public override void Initialize()
  {
    PrincipalsList = null;
    LastSetPrincipalRolesSucceeded = false;
    LastSetPrincipalAgentScopesSucceeded = false;
    Search = null;
    SortBy = PrincipalListSortColumn.CreatedAt;
    SortDescending = false;
    Current = null;
    LoadError = null;
    ListLoadError = null;
  }
}
