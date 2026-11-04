#region Purpose
// Domain aggregate for a role: name, description, and the permission ids and agent scopes it owns.
#endregion

#region Design
// Hand-written in the Profile exemplar's shape (task 461 final resolution: domain-first, no
// generator). Private constructor + two fail-closed Create overloads: Create(name, description)
// mints a RoleId for an admin-created role; Create(RoleId, ...) is the fixed-id path the stores
// use for the four product roles and for snapshot copies. Empty RoleId is rejected.
// Named mutations only: UpdateDetails replaces Name and Description as a pair (UpdateRole sends
// both); SetPermissions and SetAgentScopes are full replace, matching the IIdempotentCommand PUTs
// that call them, so a retry leaves the same set. Duplicates collapse (ordinal) and the requested
// order is kept. A child already present is kept as the same instance and only missing values are
// new, so EF sees unchanged rows as unchanged and deletes/inserts only the difference.
// Children are value records over private backing lists (PermissionList, AgentScopeList); the EF
// mapping uses field access on those names. Catalog membership is checked against RoleCatalog,
// the domain copy of PermissionIds.All and AgentScopes.All (see its Design for the agreement test).
// Product-role delete guard is a domain rule here, not a store guard: IsProductRole reads
// RoleCatalog.ProductRoleIds, and EnsureRemovable throws for a product role. Both stores call
// EnsureRemovable inside RemoveAsync, so no path deletes Member/Operator/Administrator/Developer;
// the DeleteRole handler checks IsProductRole first and answers 409.
// Protected core (Administrator keeps admin.*) is not a domain rule: AdminLockoutGuards in the
// application layer owns it and answers 409 before SetPermissions runs.
// The nested private Invariants validator is the save-time half (DomainInvariantsGuard runs it
// from the SaveChanges hook; TWA0011/TWA0012 enforce the shape).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Domain;

using FluentValidation;

public sealed class Role : Entity<RoleId>, IAggregateRoot
{
  private readonly List<PermissionGrant> PermissionList = [];
  private readonly List<AgentScopeGrant> AgentScopeList = [];

  private Role(RoleId id, string name, string description)
    : base(id)
  {
    Name = name;
    Description = description;
  }

  public string Name { get; private set; }
  public string Description { get; private set; }

  public IReadOnlyList<PermissionGrant> Permissions => PermissionList;
  public IReadOnlyList<AgentScopeGrant> AgentScopes => AgentScopeList;

  /// <summary>True for the four product roles (RoleIds.All); those cannot be deleted.</summary>
  public bool IsProductRole => RoleCatalog.IsProductRole(Id);

  public static Role Create(string name, string description)
  {
    EnsureDetails(name, description);
    return new Role(RoleId.New(), name, description);
  }

  /// <summary>Create a role with a fixed id (product roles and store snapshots).</summary>
  public static Role Create(RoleId id, string name, string description)
  {
    if (id.IsEmpty)
    {
      throw new ArgumentException("RoleId must be non-empty.", nameof(id));
    }

    EnsureDetails(name, description);
    return new Role(id, name, description);
  }

  public void UpdateDetails(string name, string description)
  {
    EnsureDetails(name, description);
    Name = name;
    Description = description;
  }

  /// <summary>Replace the permission ids (empty clears). Each id must be in the permission catalog.</summary>
  public void SetPermissions(IEnumerable<string> permissionIds)
  {
    ArgumentNullException.ThrowIfNull(permissionIds);
    List<string> requested = [.. permissionIds.Distinct(StringComparer.Ordinal)];
    foreach (string permissionId in requested)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(permissionId, nameof(permissionIds));
      if (!RoleCatalog.IsPermissionId(permissionId))
      {
        throw new ArgumentException($"'{permissionId}' is not a known permission id.", nameof(permissionIds));
      }
    }

    List<PermissionGrant> next =
    [
      .. requested.Select(permissionId =>
        PermissionList.Find(existing => existing.PermissionId.Equals(permissionId, StringComparison.Ordinal))
        ?? new PermissionGrant(permissionId))
    ];
    PermissionList.Clear();
    PermissionList.AddRange(next);
  }

  /// <summary>Replace the agent scopes (empty clears). Each scope must be in the agent-scope catalog.</summary>
  public void SetAgentScopes(IEnumerable<string> scopes)
  {
    ArgumentNullException.ThrowIfNull(scopes);
    List<string> requested = [.. scopes.Distinct(StringComparer.Ordinal)];
    foreach (string scope in requested)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(scope, nameof(scopes));
      if (!RoleCatalog.IsAgentScope(scope))
      {
        throw new ArgumentException($"'{scope}' is not a known agent scope.", nameof(scopes));
      }
    }

    List<AgentScopeGrant> next =
    [
      .. requested.Select(scope =>
        AgentScopeList.Find(existing => existing.Scope.Equals(scope, StringComparison.Ordinal))
        ?? new AgentScopeGrant(scope))
    ];
    AgentScopeList.Clear();
    AgentScopeList.AddRange(next);
  }

  /// <summary>Throws when this is a product role. Stores call it before removing a role.</summary>
  public void EnsureRemovable()
  {
    if (IsProductRole)
    {
      throw new InvalidOperationException($"Role '{Id}' is a product role and cannot be deleted.");
    }
  }

  private static void EnsureDetails(string name, string description)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentException.ThrowIfNullOrWhiteSpace(description);
  }

  private sealed class Invariants : AbstractValidator<Role>
  {
    public Invariants()
    {
      RuleFor(role => role.Name).NotEmpty();
      RuleFor(role => role.Description).NotEmpty();
      RuleForEach(role => role.Permissions)
        .Must(permission => RoleCatalog.IsPermissionId(permission.PermissionId))
        .WithMessage("Each permission must be in the permission catalog.");
      RuleForEach(role => role.AgentScopes)
        .Must(scope => RoleCatalog.IsAgentScope(scope.Scope))
        .WithMessage("Each agent scope must be in the agent-scope catalog.");
    }
  }
}
