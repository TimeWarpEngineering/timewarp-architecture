#region Purpose
// EF Core IRoleAgentScopeStore: reads role agent scopes from the Role aggregate's owned rows.
#endregion

#region Design
// Task 473: same split as EfRolePermissionStore. Projects identity.role_agent_scopes through the
// Role mapping instead of loading aggregates; agent-scope approval calls this at token issuance.
// The batch read omits roles with no scopes. Writes go through IRoleStore. Scoped; PostgresDbModule
// registers it in place of InMemoryRoleAgentScopeStore when connected.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Infrastructure;

using Microsoft.EntityFrameworkCore;
using TimeWarp.Architecture.Authorization;
using TimeWarp.Architecture.Features.Admin.Roles.Domain;
using TimeWarp.Architecture.Persistence;

/// <summary>Postgres-backed role agent-scope reads over the Role aggregate's rows.</summary>
public sealed class EfRoleAgentScopeStore : IRoleAgentScopeStore
{
  private readonly PostgresDbContext Db;

  public EfRoleAgentScopeStore(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task<IReadOnlyList<string>> GetScopesForRoleAsync(
    Guid roleId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var id = RoleId.From(roleId);

    return await Db.Roles
      .AsNoTracking()
      .Where(role => role.Id == id)
      .SelectMany(role => role.AgentScopes)
      .Select(scope => scope.Scope)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetScopesForRolesAsync(
    IReadOnlyCollection<Guid> roleIds,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(roleIds);
    cancellationToken.ThrowIfCancellationRequested();

    RoleId[] wanted = [.. roleIds.Distinct().Select(RoleId.From)];
    if (wanted.Length == 0)
    {
      return new Dictionary<Guid, IReadOnlyList<string>>();
    }

    var rows = await Db.Roles
      .AsNoTracking()
      .Where(role => wanted.Contains(role.Id))
      .SelectMany(role => role.AgentScopes, (role, scope) => new { role.Id, scope.Scope })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows
      .GroupBy(row => row.Id.Value)
      .ToDictionary(
        group => group.Key,
        group => (IReadOnlyList<string>)[.. group.Select(row => row.Scope)]);
  }
}
