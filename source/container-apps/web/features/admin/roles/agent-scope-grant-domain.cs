#region Purpose
// One agent scope a Role may approve on an agent token: an owned value child of the Role aggregate.
#endregion

#region Design
// Value record (equality by Scope) owned by Role and mapped onto the existing
// identity.role_agent_scopes (RoleId, Scope) rows; no surrogate id. The constructor checks shape
// only (non-blank, MaxLength); catalog membership is the aggregate's rule (Role.SetAgentScopes and
// Role.Invariants). MaxLength 64 equals the column length and the issuance contract's per-entry
// ceiling.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Domain;

public sealed record AgentScopeGrant
{
  public const int MaxLength = 64;

  public AgentScopeGrant(string scope)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(scope);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(scope.Length, MaxLength, nameof(scope));
    Scope = scope;
  }

  public string Scope { get; }
}
