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

## Configured vs enabled

**Configured** (deploy-time, secret-bearing) stays in host `Authentication:Entra:*`: client id,
secret, authority tenant, `PublicOrigin`, and `Enabled` as the **scheme-registration gate**.
`dev entra setup` still writes those user secrets.

**Enabled for users** is runtime admin policy on the `SiteSettings` singleton (`ISiteSettingsStore`).
First boot copies `Enabled`, `AllowBootstrap`, and `TrustedTenants` into settings once; after that
the Settings page is the source of truth for those three. Disabling sign-in at runtime refuses
**new** challenges (403 `Sign-in disabled`); existing Entra credentials and sessions stay.

`IEntraSignInPolicy` is the application seam products replace in DI (crunchit 008-004) without
touching the named `entra` scheme. Do not fold `PublicOrigin`, client id, secret, or authority
tenant into `SiteSettings`.
