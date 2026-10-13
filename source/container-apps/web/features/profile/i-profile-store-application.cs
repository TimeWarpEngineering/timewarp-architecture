#region Purpose
// Application port for Profile persistence — handlers never take DbContext.
#endregion

#region Design
// Task 148 D4: dual-mode store behind IProfileStore so application stays free of EF.
//   - InMemoryProfileStore singleton is the zero-infra default (InMemoryProfileStoresModule).
//   - EfProfileStore scoped replaces it when PostgresDbModule sees a connection string.
// Find/Add for GetProfile create-if-missing; Update for UpdateProfile (task 205).
// ProfileId is 1:1 with the authenticated UserId (Profile.Create(ProfileId, …)).
// Task 328: FindMany/Search are read-only admin directory queries behind IProfileDirectory.
// Search is a case-insensitive contains match on DisplayName or Email, not paged: the admin
// list is small.
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Application;

using TimeWarp.Architecture.Features.Profiles.Domain;

/// <summary>Durable Profile lookup and insert (application port).</summary>
public interface IProfileStore
{
  /// <summary>Returns the profile or null when no row exists for <paramref name="id"/>.</summary>
  Task<Profile?> FindAsync(ProfileId id, CancellationToken cancellationToken = default);

  /// <summary>Returns the profiles that exist for <paramref name="ids"/>; missing ids are absent.</summary>
  Task<IReadOnlyList<Profile>> FindManyAsync(IReadOnlyCollection<ProfileId> ids, CancellationToken cancellationToken = default);

  /// <summary>Profiles whose DisplayName or Email contains <paramref name="term"/>, ignoring case.</summary>
  Task<IReadOnlyList<Profile>> SearchAsync(string term, CancellationToken cancellationToken = default);

  /// <summary>
  /// Inserts a new profile. Throws <see cref="InvalidOperationException"/> when the id
  /// already exists (callers that race create-if-missing re-find after this).
  /// </summary>
  Task AddAsync(Profile profile, CancellationToken cancellationToken = default);

  /// <summary>
  /// Persists mutations already applied on <paramref name="profile"/>. Throws
  /// <see cref="InvalidOperationException"/> when the id is unknown.
  /// </summary>
  Task UpdateAsync(Profile profile, CancellationToken cancellationToken = default);
}
