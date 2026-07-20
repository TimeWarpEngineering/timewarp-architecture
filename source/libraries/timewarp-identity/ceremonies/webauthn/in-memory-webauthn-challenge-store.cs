#region Purpose
// Thread-safe in-memory IWebAuthnChallengeStore for unit tests and single-instance hosts.
#endregion

#region Design
// Single-instance semantics only: challenges live in process memory, so a multi-instance/scaled-out
// host would let a request land on an instance that never issued the challenge it is being asked to
// consume. A distributed store (e.g. Redis-backed) is explicit future work, not attempted here.
// Keyed by the challenge's own base64url encoding — the 32 random bytes are already
// collision-resistant, so no separate id/GUID is minted. TryConsume removes the entry via
// ConcurrentDictionary.TryRemove BEFORE checking ceremony type/expiry: removal is what makes
// consumption one-time and atomic under concurrent callers; the type/expiry checks after removal
// only decide whether the (now-gone-either-way) challenge counts as a successful consume.
// Issue prunes expired entries first (bounds unbounded growth from abandoned ceremonies under
// normal operation) and then evicts the single oldest-by-expiry entry if still at MaxEntries — a
// cheap DoS bound, not a substitute for real rate limiting (task 104-015).
// Delegation refactor (task 104-004): the prune/evict/consume body above now lives once in
// InMemoryChallengeStoreCore<TCeremonyType> (ceremonies/in-memory-challenge-store-core.cs),
// shared with InMemoryAgentKeyChallengeStore — this type is a thin, behavior-preserving wrapper.
// The public surface (this ctor's parameters/defaults, Issue/TryConsume signatures) is UNCHANGED;
// the existing WebAuthn challenge-store tests re-run unmodified as the regression pin for that claim.
#endregion

namespace TimeWarp.Identity;

public sealed class InMemoryWebAuthnChallengeStore : IWebAuthnChallengeStore
{
  private readonly InMemoryChallengeStoreCore<WebAuthnCeremonyType> Core;

  public InMemoryWebAuthnChallengeStore(TimeProvider? timeProvider = null, TimeSpan? timeToLive = null, int maxEntries = 10_000)
  {
    Core = new InMemoryChallengeStoreCore<WebAuthnCeremonyType>(timeProvider, timeToLive, maxEntries);
  }

  public byte[] Issue(WebAuthnCeremonyType ceremonyType) => Core.Issue(ceremonyType);

  public bool TryConsume(WebAuthnCeremonyType ceremonyType, byte[] challenge) => Core.TryConsume(ceremonyType, challenge);
}
