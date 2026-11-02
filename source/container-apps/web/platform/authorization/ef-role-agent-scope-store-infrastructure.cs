#region Purpose
// EF Core IRoleAgentScopeStore: durable role → agent-scope grants behind the postgres flag.
#endregion

#region Design
// Task 466: same replace-set semantics as EfRolePermissionStore. Scoped lifetime because it
// depends on scoped PostgresDbContext. InMemoryIdentityStoresModule registers the singleton
// in-memory store; PostgresDbModule replaces it when connected.
#endregion

namespace TimeWarp.Architecture.Authorization;

using Microsoft.EntityFrameworkCore;
using TimeWarp.Architecture.Persistence;

/// <summary>Postgres-backed role → agent-scope grant store.</summary>
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

    List<string> scopes = await Db.Set<RoleAgentScopeGrant>()
      .AsNoTracking()
      .Where(row => row.RoleId == roleId)
      .Select(row => row.Scope)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return scopes;
  }

  public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetScopesForRolesAsync(
    IReadOnlyCollection<Guid> roleIds,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(roleIds);
    cancellationToken.ThrowIfCancellationRequested();

    Guid[] distinct = roleIds.Distinct().ToArray();
    if (distinct.Length == 0)
    {
      return new Dictionary<Guid, IReadOnlyList<string>>();
    }

    List<RoleAgentScopeGrant> rows = await Db.Set<RoleAgentScopeGrant>()
      .AsNoTracking()
      .Where(row => distinct.Contains(row.RoleId))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows
      .GroupBy(row => row.RoleId)
      .ToDictionary(
        group => group.Key,
        group => (IReadOnlyList<string>)group.Select(row => row.Scope).ToArray());
  }

  public async Task SetScopesForRoleAsync(
    Guid roleId,
    IReadOnlyList<string> scopes,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(scopes);
    cancellationToken.ThrowIfCancellationRequested();

    List<RoleAgentScopeGrant> existing = await Db.Set<RoleAgentScopeGrant>()
      .Where(row => row.RoleId == roleId)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    if (existing.Count > 0)
    {
      Db.Set<RoleAgentScopeGrant>().RemoveRange(existing);
    }

    foreach (string scope in scopes.Distinct(StringComparer.Ordinal))
    {
      Db.Set<RoleAgentScopeGrant>().Add(new RoleAgentScopeGrant
      {
        RoleId = roleId,
        Scope = scope
      });
    }

    await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }
}
