# TimeWarp.Identity — library layout

Namespaces do not track folders (everything is `TimeWarp.Identity`); folders exist for reader
cohesion. The placement rules:

| Folder | Holds | Rule |
|--------|-------|------|
| `principals/` | Principal aggregate + its typed id and enums | Domain entity per folder |
| `credentials/` | Credential aggregate + typed id, enum, Entra handle/issuer helpers | Domain entity per folder |
| `settings/` | SiteSettings singleton aggregate + typed id + PasskeyPromptMode | Domain entity per folder — runtime admin policy, not configuration |
| `persistence/` | `IPrincipalStore`, `ISiteSettingsStore`, in-memory impls, `ConcurrencyConflictException` | The **durable domain-data seam** — what a database will hold; the port hosts swap for EF/Postgres |
| `ceremonies/webauthn/` | Passkey verifier + its challenge store | Feature cohesion: everything the ceremony needs, including its **ephemeral** protocol state |
| `ceremonies/agent-key/` | Agent-key proof + its challenge store | Same rule |
| `ceremonies/` (root) | `InMemoryChallengeStoreCore` shared by both challenge stores | Shared ceremony machinery |
| `tokens/` | Scopes, grants, `IAgentTokenStore`, in-memory impl | Feature cohesion; grants are ephemeral (TTL'd) |

The store-placement distinction, stated once: **`persistence/` = what the database will hold;
feature folders = what the feature needs to run.** Challenge nonces and token grants are TTL'd
protocol state that will never be EF entities — a distributed deployment would back them with a
cache (Redis), not tables — so their stores live beside the ceremonies/tokens they serve rather
than in `persistence/`.

## Principal merge

`IPrincipalStore.MergePrincipalAsync(source, target)` re-parents every **active** credential
from source onto target (revoked rows stay on source for audit), raises target trust to
`max(source, target)`, copies `DisplayName` only when target's is empty, then
`Principal.MergeInto(target)` — `MergedIntoPrincipalId` is set and `IsActive` is false.
`Credential.ReparentTo` changes `PrincipalId` only; Type and Handle stay immutable so the
authenticator's credential id still looks up. Un-merge is out of scope. A merged principal
cannot sign in; passkey login on a moved credential authenticates as the surviving principal.

## Configured vs enabled

**Configured** (deploy-time, secret-bearing) stays in host `Authentication:Entra:*`: client id,
secret, authority tenant, `PublicOrigin`, and `Enabled` as the **scheme-registration gate**.
`dev entra setup` still writes those user secrets.

**Enabled for users** is runtime admin policy on the `SiteSettings` singleton (`ISiteSettingsStore`).
First boot copies `Enabled` and `AllowBootstrap` into settings once; after that
`/Admin/Authentication` is the source of truth for those two. Trust is the configured
`Authentication:Entra:TenantId` (token `tid` must GUID-equal that tenant).
`organizations` / `common` authority is not supported for bootstrap or link — those
tickets are refused with 403 `Untrusted tenant`. Disabling sign-in at runtime refuses
**new** challenges (403 `Sign-in disabled`); existing Entra credentials and sessions stay.

`IEntraSignInPolicy` is the application seam products replace in DI (crunchit 008-004) without
touching the named `entra` scheme. Do not fold `PublicOrigin`, client id, secret, or authority
tenant into `SiteSettings`.
