#region Purpose
// Thread-safe in-memory IProfileStore for zero-infra / skip-mode (default without Postgres).
#endregion

#region Design
// Task 148 D4: ConcurrentDictionary keyed by ProfileId; process-lifetime singleton matches
// InMemoryPrincipalStore registration. PostgresDbModule swaps to scoped EfProfileStore when a
// connection string is present. Add throws on duplicate so GetProfile create-if-missing can
// re-find after a concurrent insert race (same contract as EF unique-PK path).
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Application;

using System.Collections.Concurrent;
using TimeWarp.Architecture.Features.Profiles.Domain;

/// <summary>In-memory Profile store (zero-infra default).</summary>
public sealed class InMemoryProfileStore : IProfileStore
{
  private readonly ConcurrentDictionary<ProfileId, Profile> Profiles = new();

  public Task<Profile?> FindAsync(ProfileId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    Profiles.TryGetValue(id, out Profile? profile);
    return Task.FromResult(profile);
  }

  public Task AddAsync(Profile profile, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(profile);
    cancellationToken.ThrowIfCancellationRequested();

    if (!Profiles.TryAdd(profile.Id, profile))
    {
      throw new InvalidOperationException($"Profile '{profile.Id}' already exists.");
    }

    return Task.CompletedTask;
  }

  public Task UpdateAsync(Profile profile, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(profile);
    cancellationToken.ThrowIfCancellationRequested();

    if (!Profiles.ContainsKey(profile.Id))
    {
      throw new InvalidOperationException($"Profile '{profile.Id}' does not exist.");
    }

    Profiles[profile.Id] = profile;
    return Task.CompletedTask;
  }
}
