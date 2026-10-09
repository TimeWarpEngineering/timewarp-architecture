#region Purpose
// Reads the signed-in principal's existing profile email without coupling the caller to Profile.
#endregion

#region Design
// Feedback filing must not reference the profile slice (TWA0009, same assembly). The port lives
// outside SliceRoot. The profile slice implements it against IProfileStore. A missing profile
// or a blank email is null — callers do not create a profile as a side effect.
#endregion

namespace TimeWarp.Architecture.Abstractions;

public interface IProfileEmailLookup
{
  Task<string?> FindEmailAsync(Guid principalId, CancellationToken cancellationToken = default);
}
