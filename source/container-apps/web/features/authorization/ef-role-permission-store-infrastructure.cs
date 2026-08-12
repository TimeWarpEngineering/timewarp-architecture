#region Purpose
// EF Core IRolePermissionStore: durable role→permission grants behind the postgres flag.
#endregion

#region Design
// Task 182-001: mirrors EfPrincipalRoleStore dual-mode semantics:
//   - Get: empty list when no rows
//   - Set: replace-set (delete all for role, insert Distinct permission ids); empty clears
// Scoped lifetime: depends on scoped PostgresDbContext. InMemoryIdentityStoresModule still
// registers singleton InMemoryRolePermissionStore (seeded); PostgresDbModule replaces when connected.
// Seed data for product roles lives in the EF migration (InsertData), not in this type.
// Reads AsNoTracking. Set uses a single SaveChanges after remove+add.
#endregion

namespace TimeWarp.Architecture.Features.Authorization.Infrastructure;

using Microsoft.EntityFrameworkCore;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Persistence;

/// <summary>Postgres-backed role → permission grant store.</summary>
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

    List<string> permissions = await Db.Set<RolePermissionGrant>()
      .AsNoTracking()
      .Where(row => row.RoleId == roleId)
      .Select(row => row.PermissionId)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return permissions;
  }

  public async Task SetPermissionIdsForRoleAsync(
    Guid roleId,
    IReadOnlyList<string> permissionIds,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(permissionIds);
    cancellationToken.ThrowIfCancellationRequested();

    List<RolePermissionGrant> existing = await Db.Set<RolePermissionGrant>()
      .Where(row => row.RoleId == roleId)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    if (existing.Count > 0)
    {
      Db.Set<RolePermissionGrant>().RemoveRange(existing);
    }

    string[] distinct = permissionIds.Distinct(StringComparer.Ordinal).ToArray();
    foreach (string permissionId in distinct)
    {
      Db.Set<RolePermissionGrant>().Add(new RolePermissionGrant
      {
        RoleId = roleId,
        PermissionId = permissionId
      });
    }

    await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }
}
