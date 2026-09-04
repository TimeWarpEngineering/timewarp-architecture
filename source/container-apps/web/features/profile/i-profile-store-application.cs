#region Purpose
// Application port for Profile persistence — handlers never take DbContext.
#endregion

#region Design
// Task 148 D4: dual-mode store behind IProfileStore so application stays free of EF.
//   - InMemoryProfileStore singleton is the zero-infra default (InMemoryProfileStoresModule).
//   - EfProfileStore scoped replaces it when PostgresDbModule sees a connection string.
// Find/Add for GetProfile create-if-missing; Update for UpdateProfile (task 205).
// ProfileId is 1:1 with the authenticated UserId (Profile.Create(ProfileId, …)).
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Application;

using TimeWarp.Architecture.Features.Profiles.Domain;

/// <summary>Durable Profile lookup and insert (application port).</summary>
public interface IProfileStore
{
  /// <summary>Returns the profile or null when no row exists for <paramref name="id"/>.</summary>
  Task<Profile?> FindAsync(ProfileId id, CancellationToken cancellationToken = default);

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
