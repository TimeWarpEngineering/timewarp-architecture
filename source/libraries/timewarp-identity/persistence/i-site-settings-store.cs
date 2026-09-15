#region Purpose
// Persistence port for the singleton site-settings aggregate — hosts supply EF; tests use in-memory.
#endregion

#region Design
// Task 219-006 copies IPrincipalStore: snapshot-on-get, store-owned Version CAS, ConcurrencyConflictException
// on mismatch. Schema lives alongside identity (table identity.site_settings), never in principals.
// GetAsync returns null when the store is empty — seed is a host concern (first-run copy from
// Authentication:Entra:Enabled / AllowBootstrap / TrustedTenants), not this port. AddAsync is the
// empty-store insert (throws if the singleton already exists). UpdateAsync is the admin write path
// the task names: compare incoming Version to stored; mismatch throws and leaves stored state
// untouched; match persists Snapshot(EntityVersion.Next). The caller's in-hand instance is not
// advanced. Unknown id (not the singleton, or missing row on Update) is InvalidOperationException.
// Snapshot-on-get is required so two callers can disagree about Version. Add persists Version as-is
// (0 for Create). Products replace IEntraSignInPolicy in DI; they do not have to replace this store.
#endregion

namespace TimeWarp.Identity;

public interface ISiteSettingsStore
{
  /// <summary>The singleton row, or null when the store has never been seeded.</summary>
  Task<SiteSettings?> GetAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Inserts the singleton. Throws <see cref="InvalidOperationException"/> when a row already exists.
  /// </summary>
  Task AddAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default);

  /// <summary>
  /// Persists a snapshot when <paramref name="siteSettings"/>.<see cref="Entity{SiteSettingsId}.Version"/>
  /// matches the stored row. Throws <see cref="ConcurrencyConflictException"/> on mismatch.
  /// </summary>
  Task UpdateAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default);
}
