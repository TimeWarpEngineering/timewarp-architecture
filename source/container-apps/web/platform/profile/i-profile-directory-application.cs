#region Purpose
// Read-only batch lookup and search over every principal's profile, for admin surfaces.
#endregion

#region Design
// Task 328. Why a platform port: Admin.Principals must not reference the Profile product slice
// (TWA0009, same assembly), so the port lives outside SliceRoot, like IProfileEmailLookup, and
// the profile slice implements it against IProfileStore.
// PII: entries carry display name and email. The port does no authorization; the caller gates
// on admin.profiles.read before asking and before returning any field.
// Read-only: there is no create-on-read, unlike GetProfile. A principal with no profile row is
// absent from FindManyAsync, and Guid.Empty is ignored.
// SearchAsync is a case-insensitive contains match on display name or email and returns
// principal ids. A blank term is a caller bug and throws ArgumentException.
#endregion

namespace TimeWarp.Architecture.Abstractions;

public interface IProfileDirectory
{
  Task<IReadOnlyDictionary<Guid, ProfileDirectoryEntry>> FindManyAsync(
    IReadOnlyCollection<Guid> principalIds,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<Guid>> SearchAsync(string term, CancellationToken cancellationToken = default);
}
