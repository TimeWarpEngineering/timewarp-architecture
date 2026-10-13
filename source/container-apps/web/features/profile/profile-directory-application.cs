#region Purpose
// IProfileDirectory backed by the current IProfileStore.
#endregion

#region Design
// Task 328. Scoped so it follows the store swap (singleton in-memory, scoped EF), like
// ProfileEmailLookup. Read-only: nothing here inserts a profile. Guid.Empty is dropped before
// the store is asked. No authorization here; the admin handlers gate on admin.profiles.read.
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Application;

using TimeWarp.Architecture.Features.Profiles.Domain;

public sealed class ProfileDirectory : IProfileDirectory
{
  private readonly IProfileStore ProfileStore;

  public ProfileDirectory(IProfileStore profileStore)
  {
    ProfileStore = profileStore;
  }

  public async Task<IReadOnlyDictionary<Guid, ProfileDirectoryEntry>> FindManyAsync(
    IReadOnlyCollection<Guid> principalIds,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(principalIds);
    ProfileId[] ids = [.. principalIds.Where(static id => id != Guid.Empty).Distinct().Select(static id => ProfileId.From(id))];
    if (ids.Length == 0)
    {
      return new Dictionary<Guid, ProfileDirectoryEntry>();
    }

    IReadOnlyList<Profile> profiles = await ProfileStore.FindManyAsync(ids, cancellationToken).ConfigureAwait(false);
    return profiles.ToDictionary(static profile => profile.Id.Value, ToEntry);
  }

  public async Task<IReadOnlyList<Guid>> SearchAsync(string term, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(term);
    IReadOnlyList<Profile> profiles = await ProfileStore.SearchAsync(term.Trim(), cancellationToken).ConfigureAwait(false);
    return [.. profiles.Select(static profile => profile.Id.Value)];
  }

  private static ProfileDirectoryEntry ToEntry(Profile profile) =>
    new(
      profile.Id.Value,
      profile.DisplayName,
      profile.Email,
      profile.Language,
      profile.Region,
      profile.Theme,
      profile.Notifications);
}
