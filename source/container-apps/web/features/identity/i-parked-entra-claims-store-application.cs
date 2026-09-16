#region Purpose
// Ephemeral store for parked Entra bootstrap claims keyed by an opaque id (cookie value).
#endregion

#region Design
// Same lifetime class as WebAuthn challenges: in-memory, TTL'd, single-use consume. Peek
// (TryGet) lets the choose page show expired vs ready without burning the ticket. Redis later
// if multi-replica requires shared park state.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public interface IParkedEntraClaimsStore
{
  string Park(ParkedEntraClaims claims);
  bool TryGet(string id, out ParkedEntraClaims? claims);
  bool TryConsume(string id, out ParkedEntraClaims? claims);
}
