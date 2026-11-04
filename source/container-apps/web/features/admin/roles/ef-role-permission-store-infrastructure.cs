#region Purpose
// EF Core IRolePermissionStore: reads a role's permission ids from the Role aggregate's owned rows.
#endregion

#region Design
// Task 473: the platform read port implemented by the Roles slice (platform never references a
// product slice). Projects identity.role_permissions through the Role mapping (SelectMany over
// the owned collection) instead of loading the aggregate: the permission evaluator calls this on
// the request path. Empty when the role has no rows or does not exist. Writes go through
// IRoleStore. Scoped (depends on scoped PostgresDbContext); PostgresDbModule registers it in
// place of InMemoryRolePermissionStore when connected.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Infrastructure;

using Microsoft.EntityFrameworkCore;
using TimeWarp.Architecture.Authorization;
using TimeWarp.Architecture.Features.Admin.Roles.Domain;
using TimeWarp.Architecture.Persistence;

/// <summary>Postgres-backed role permission reads over the Role aggregate's rows.</summary>
public sealed class EfRolePermissionStore : IRolePermissionStore
{
  private readonly PostgresDbContext Db;

  public EfRolePermissionStore(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task<IReadOnlyList<string>> GetPermissionIdsForRoleAsync(
    Guid roleId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var id = RoleId.From(roleId);

    return await Db.Roles
      .AsNoTracking()
      .Where(role => role.Id == id)
      .SelectMany(role => role.Permissions)
      .Select(permission => permission.PermissionId)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }
}
