#region Purpose
// IProfileEmailLookup backed by the current IProfileStore.
#endregion

#region Design
// Scoped so the lookup follows the store swap (singleton in-memory, scoped EF). Empty principal
// ids and a missing or blank Profile.Email return null. This method does not insert a profile.
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Application;

using TimeWarp.Architecture.Features.Profiles.Domain;

public sealed class ProfileEmailLookup : IProfileEmailLookup
{
  private readonly IProfileStore ProfileStore;

  public ProfileEmailLookup(IProfileStore profileStore)
  {
    ProfileStore = profileStore;
  }

  public async Task<string?> FindEmailAsync(Guid principalId, CancellationToken cancellationToken = default)
  {
    if (principalId == Guid.Empty)
    {
      return null;
    }

    Profile? profile = await ProfileStore.FindAsync(ProfileId.From(principalId), cancellationToken)
      .ConfigureAwait(false);
    if (string.IsNullOrWhiteSpace(profile?.Email))
    {
      return null;
    }

    return profile.Email;
  }
}
