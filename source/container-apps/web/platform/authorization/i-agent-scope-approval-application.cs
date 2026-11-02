#region Purpose
// The set of agent scopes a principal may name at token issuance, and where each one came from.
#endregion

#region Design
// Task 466: identity:read is always approved. Everything else must be on an effective role's
// agent-scope list or granted on the principal. Effective roles are the same set the admin
// Principal page shows (a roleless human resolves to Member; a roleless agent resolves to no
// role). Human permission ids are not consulted. A removed approval does not revoke a token
// already issued.
#endregion

namespace TimeWarp.Architecture.Authorization;

using TimeWarp.Identity;

/// <summary>Where one approved agent scope came from.</summary>
public enum AgentScopeGrantKind
{
  Default = 0,
  Role = 1,
  Principal = 2,
}

/// <summary>One source of an approved agent scope.</summary>
public sealed class AgentScopeGrantSource
{
  public AgentScopeGrantSource(AgentScopeGrantKind kind, Guid? roleId)
  {
    Kind = kind;
    RoleId = roleId;
  }

  public AgentScopeGrantKind Kind { get; }

  public Guid? RoleId { get; }
}

/// <summary>One scope the principal may request, with every source that approved it.</summary>
public sealed class ApprovedAgentScope
{
  public ApprovedAgentScope(string scope, IReadOnlyList<AgentScopeGrantSource> sources)
  {
    Scope = scope;
    Sources = sources;
  }

  public string Scope { get; }

  public IReadOnlyList<AgentScopeGrantSource> Sources { get; }
}

/// <summary>Principal grants plus the effective approved set.</summary>
public sealed class AgentScopeApprovalSnapshot
{
  public AgentScopeApprovalSnapshot(
    IReadOnlyList<string> principalGrants,
    IReadOnlyList<ApprovedAgentScope> effective)
  {
    PrincipalGrants = principalGrants;
    Effective = effective;
  }

  public IReadOnlyList<string> PrincipalGrants { get; }

  public IReadOnlyList<ApprovedAgentScope> Effective { get; }

  public bool IsApproved(string scope) =>
    Effective.Any(item => string.Equals(item.Scope, scope, StringComparison.Ordinal));
}

/// <summary>Computes the agent scopes a principal may request.</summary>
public interface IAgentScopeApproval
{
  Task<AgentScopeApprovalSnapshot> GetSnapshotAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default);
}
