#region Purpose
// EF Core IPrincipalAgentScopeStore: durable principal → agent-scope grants.
#endregion

#region Design
// Task 466: replace-set, scoped on PostgresDbContext. PostgresDbModule swaps this in for the
// in-memory store when a connection string is present.
#endregion

namespace TimeWarp.Architecture.Authorization;

using Microsoft.EntityFrameworkCore;
using TimeWarp.Architecture.Persistence;
using TimeWarp.Identity;

/// <summary>Postgres-backed principal → agent-scope grant store.</summary>
public sealed class EfPrincipalAgentScopeStore : IPrincipalAgentScopeStore
{
  private readonly PostgresDbContext Db;

  public EfPrincipalAgentScopeStore(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task<IReadOnlyList<string>> GetScopesAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    List<string> scopes = await Db.Set<PrincipalAgentScopeGrant>()
      .AsNoTracking()
      .Where(row => row.PrincipalId == principalId)
      .Select(row => row.Scope)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return scopes;
  }

  public async Task SetScopesAsync(
    PrincipalId principalId,
    IReadOnlyList<string> scopes,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(scopes);
    cancellationToken.ThrowIfCancellationRequested();

    List<PrincipalAgentScopeGrant> existing = await Db.Set<PrincipalAgentScopeGrant>()
      .Where(row => row.PrincipalId == principalId)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    if (existing.Count > 0)
    {
      Db.Set<PrincipalAgentScopeGrant>().RemoveRange(existing);
    }

    foreach (string scope in scopes.Distinct(StringComparer.Ordinal))
    {
      Db.Set<PrincipalAgentScopeGrant>().Add(new PrincipalAgentScopeGrant
      {
        PrincipalId = principalId,
        Scope = scope
      });
    }

    await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }
}
