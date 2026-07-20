# TimeWarp.Identity — library layout

Namespaces do not track folders (everything is `TimeWarp.Identity`); folders exist for reader
cohesion. The placement rules:

| Folder | Holds | Rule |
|--------|-------|------|
| `principals/` | Principal aggregate + its typed id and enums | Domain entity per folder |
| `credentials/` | Credential aggregate + its typed id and enum | Domain entity per folder |
| `persistence/` | `IPrincipalStore`, in-memory impl, `ConcurrencyConflictException` | The **durable domain-data seam** — what a database will hold; the port hosts swap for EF/Postgres |
| `ceremonies/webauthn/` | Passkey verifier + its challenge store | Feature cohesion: everything the ceremony needs, including its **ephemeral** protocol state |
| `ceremonies/agent-key/` | Agent-key proof + its challenge store | Same rule |
| `ceremonies/` (root) | `InMemoryChallengeStoreCore` shared by both challenge stores | Shared ceremony machinery |
| `tokens/` | Scopes, grants, `IAgentTokenStore`, in-memory impl | Feature cohesion; grants are ephemeral (TTL'd) |

The store-placement distinction, stated once: **`persistence/` = what the database will hold;
feature folders = what the feature needs to run.** Challenge nonces and token grants are TTL'd
protocol state that will never be EF entities — a distributed deployment would back them with a
cache (Redis), not tables — so their stores live beside the ceremonies/tokens they serve rather
than in `persistence/`.
