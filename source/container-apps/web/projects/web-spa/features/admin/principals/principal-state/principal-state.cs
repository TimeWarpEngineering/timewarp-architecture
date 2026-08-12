#region Purpose
// SPA state for the admin principals list and inline role assignment.
#endregion

#region Design
// Principals null = no snapshot yet; empty = loaded with zero rows.
// In-flight fetch is [TrackAction] on FetchPrincipals — pages use IsAnyActive, not null.
// DraftRoleIds tracks multi-select edits per principal before Save → SetPrincipalRoles.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using static ListPrincipals;

[StateAccess]
public sealed partial class PrincipalState : State<PrincipalState>
{
  private List<PrincipalSummaryDto>? PrincipalsList { get; set; }
  private Dictionary<Guid, HashSet<Guid>> DraftRoleIds { get; set; } = new();

  public IReadOnlyList<PrincipalSummaryDto>? Principals => PrincipalsList?.AsReadOnly();

  public override void Initialize()
  {
    PrincipalsList = null;
    DraftRoleIds = new();
  }

  public IReadOnlyCollection<Guid> GetDraftRoleIds(Guid principalId) =>
    DraftRoleIds.TryGetValue(principalId, out HashSet<Guid>? set)
      ? set
      : Array.Empty<Guid>();

  public bool IsRoleSelected(Guid principalId, Guid roleId) =>
    DraftRoleIds.TryGetValue(principalId, out HashSet<Guid>? set) && set.Contains(roleId);
}
