#region Purpose
// EF Core IRoleStore: durable Role persistence (identity.roles and its owned grant rows) behind the postgres flag.
#endregion

#region Design
// Task 473, like EfProfileStore. Semantics match InMemoryRoleStore:
//   - Find / FindMany / List: AsNoTracking rows with both owned collections, or null / absent
//   - Add: insert; duplicate id (pre-check or 23505) → InvalidOperationException
//   - Update: load the tracked row, replay the named mutations (UpdateDetails, SetPermissions,
//     SetAgentScopes) from the caller's instance, SaveChanges. The full-replace setters keep rows
//     already present, so EF deletes and inserts only the difference; AggregateDbContext runs
//     Invariants and bumps Version (child-only changes resolve to the root).
//   - Remove: load tracked, Role.EnsureRemovable (product roles throw), remove; owned rows go
//     with the root.
// Every read uses AsSplitQuery: two owned collections in one query would multiply rows and log
// EF's multiple-collection warning. Scoped lifetime (depends on scoped PostgresDbContext);
// PostgresDbModule registers it in place of the singleton InMemoryRoleStore when connected.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Infrastructure;

using Microsoft.EntityFrameworkCore;
using TimeWarp.Architecture.Features.Admin.Roles.Application;
using TimeWarp.Architecture.Features.Admin.Roles.Domain;
using TimeWarp.Architecture.Persistence;

/// <summary>Postgres-backed Role store.</summary>
public sealed class EfRoleStore : IRoleStore
{
  private readonly PostgresDbContext Db;

  public EfRoleStore(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task<Role?> FindAsync(RoleId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return await Db.Roles.AsNoTracking().AsSplitQuery()
      .FirstOrDefaultAsync(role => role.Id == id, cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<Role>> FindManyAsync(
    IReadOnlyCollection<RoleId> ids,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(ids);
    cancellationToken.ThrowIfCancellationRequested();
    RoleId[] wanted = [.. ids.Distinct()];
    if (wanted.Length == 0)
    {
      return [];
    }

    return await Db.Roles.AsNoTracking().AsSplitQuery()
      .Where(role => wanted.Contains(role.Id))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return await Db.Roles.AsNoTracking().AsSplitQuery()
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(role);
    cancellationToken.ThrowIfCancellationRequested();

    bool exists = await Db.Roles.AsNoTracking()
      .AnyAsync(row => row.Id == role.Id, cancellationToken)
      .ConfigureAwait(false);
    if (exists)
    {
      throw new InvalidOperationException($"Role '{role.Id}' already exists.");
    }

    Db.Roles.Add(role);
    try
    {
      await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (DbUpdateException exception) when (IsUniqueViolation(exception))
    {
      Db.Entry(role).State = EntityState.Detached;
      throw new InvalidOperationException($"Role '{role.Id}' already exists.", exception);
    }
  }

  public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(role);
    cancellationToken.ThrowIfCancellationRequested();

    Role stored = await LoadTrackedAsync(role.Id, cancellationToken).ConfigureAwait(false);
    stored.UpdateDetails(role.Name, role.Description);
    stored.SetPermissions(role.Permissions.Select(permission => permission.PermissionId));
    stored.SetAgentScopes(role.AgentScopes.Select(scope => scope.Scope));

    await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task RemoveAsync(RoleId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    Role stored = await LoadTrackedAsync(id, cancellationToken).ConfigureAwait(false);
    stored.EnsureRemovable();
    Db.Roles.Remove(stored);

    await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private async Task<Role> LoadTrackedAsync(RoleId id, CancellationToken cancellationToken)
  {
    Role? stored = await Db.Roles.AsSplitQuery()
      .FirstOrDefaultAsync(row => row.Id == id, cancellationToken)
      .ConfigureAwait(false);
    return stored ?? throw new InvalidOperationException($"Role '{id}' does not exist.");
  }

  private static bool IsUniqueViolation(DbUpdateException exception)
  {
    // Npgsql: 23505 unique_violation. String-based so this file does not hard-depend on Npgsql types.
    Exception? current = exception;
    while (current is not null)
    {
      string text = current.GetType().FullName + " " + current.Message;
      if (text.Contains("23505", StringComparison.Ordinal)
          || text.Contains("unique", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }

      current = current.InnerException;
    }

    return false;
  }
}
