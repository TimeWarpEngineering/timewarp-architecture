#region Purpose
// Thread-safe in-memory IProfileStore for zero-infra / skip-mode (default without Postgres).
#endregion

#region Design
// Task 148 D4: ConcurrentDictionary keyed by ProfileId; process-lifetime singleton matches
// InMemoryPrincipalStore registration. PostgresDbModule swaps to scoped EfProfileStore when a
// connection string is present. Add throws on duplicate so GetProfile create-if-missing can
// re-find after a concurrent insert race (same contract as EF unique-PK path).
// Task 328: FindMany/Search scan the dictionary; Search is OrdinalIgnoreCase contains on
// DisplayName or Email, mirroring the EF store's lower-cased contains.
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

  public Task<IReadOnlyList<Profile>> FindManyAsync(
    IReadOnlyCollection<ProfileId> ids,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(ids);
    cancellationToken.ThrowIfCancellationRequested();
    List<Profile> found = [];
    foreach (ProfileId id in ids.Distinct())
    {
      if (Profiles.TryGetValue(id, out Profile? profile))
      {
        found.Add(profile);
      }
    }

    return Task.FromResult<IReadOnlyList<Profile>>(found);
  }

  public Task<IReadOnlyList<Profile>> SearchAsync(string term, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(term);
    cancellationToken.ThrowIfCancellationRequested();
    IReadOnlyList<Profile> matches =
    [
      .. Profiles.Values.Where(profile =>
        profile.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
        || profile.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)
    ];
    return Task.FromResult(matches);
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
