#region Purpose
// EF Core IPrincipalRoleStore: durable principal→role grants behind the postgres flag.
#endregion

#region Design
// Task 147-006: mirrors EfPrincipalStore dual-mode — host-owned, library stays free of roles.
// Semantics match InMemoryPrincipalRoleStore:
//   - Get: empty list when no rows (effective Member applied by resolver, not this store)
//   - Set: replace-set (delete all for principal, insert Distinct role ids); empty clears
//   - TryClaimFirstAdministrator: Serializable transaction — Any Administrator row? no → write
//     Administrator+Member for this principal. Concurrent claims: one wins, others see the row.
// Scoped lifetime: depends on scoped PostgresDbContext. InMemoryIdentityStoresModule still
// registers singleton InMemoryPrincipalRoleStore; PostgresDbModule replaces when connected.
// Reads AsNoTracking. Set uses a single SaveChanges after remove+add.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Persistence;
using TimeWarp.Identity;

/// <summary>Postgres-backed principal→role assignment store.</summary>
public sealed class EfPrincipalRoleStore : IPrincipalRoleStore
{
  private readonly PostgresDbContext Db;

  public EfPrincipalRoleStore(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task<IReadOnlyList<Guid>> GetRoleIdsAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    List<Guid> roles = await Db.Set<PrincipalRoleAssignment>()
      .AsNoTracking()
      .Where(row => row.PrincipalId == principalId)
      .Select(row => row.RoleId)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return roles;
  }

  public async Task SetRoleIdsAsync(
    PrincipalId principalId,
    IReadOnlyList<Guid> roleIds,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(roleIds);
    cancellationToken.ThrowIfCancellationRequested();

    List<PrincipalRoleAssignment> existing = await Db.Set<PrincipalRoleAssignment>()
      .Where(row => row.PrincipalId == principalId)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    if (existing.Count > 0)
    {
      Db.Set<PrincipalRoleAssignment>().RemoveRange(existing);
    }

    Guid[] distinct = roleIds.Distinct().ToArray();
    foreach (Guid roleId in distinct)
    {
      Db.Set<PrincipalRoleAssignment>().Add(new PrincipalRoleAssignment
      {
        PrincipalId = principalId,
        RoleId = roleId
      });
    }

    await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task<bool> TryClaimFirstAdministratorAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    await using IDbContextTransaction transaction = await Db.Database
      .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
      .ConfigureAwait(false);

    bool administratorExists = await Db.Set<PrincipalRoleAssignment>()
      .AsNoTracking()
      .AnyAsync(row => row.RoleId == RoleIds.Administrator, cancellationToken)
      .ConfigureAwait(false);

    if (administratorExists)
    {
      await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
      return false;
    }

    await SetRoleIdsAsync(principalId, [RoleIds.Administrator, RoleIds.Member], cancellationToken)
      .ConfigureAwait(false);

    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    return true;
  }
}
