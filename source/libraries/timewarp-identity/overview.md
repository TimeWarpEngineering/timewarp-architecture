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
| `ceremonies/` (root) | `InMemoryChallengeStoreCore` shared by both challenge stores; `LoopbackHost` (the one "http allowed only on loopback" predicate, shared by the WebAuthn origin fallback and the host's Entra `PublicOrigin` validator) | Shared ceremony machinery |
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
`/Admin/Authentication` is the source of truth for those two. Disabling sign-in at runtime refuses
**new** challenges (403 `Sign-in disabled`); existing Entra credentials and sessions stay.

`IEntraSignInPolicy` is the application seam products replace in DI without
touching the named `entra` scheme. Do not fold `PublicOrigin`, client id, secret, or authority
tenant into `SiteSettings`.

## Tenant acceptance

Trust is the site-settings tenant acceptance policy, edited on `/Admin/Authentication`:

| Mode | Token `tid` |
|------|-------------|
| Restricted | Must be in the accepted-tenant list. An empty list refuses every tenant (`403 Untrusted tenant`). One id is the single-tenant case. |
| Any | Any non-empty GUID. |

`Authentication:Entra:TenantId` is only the OIDC authority segment (`{instance}/{tenant}/v2.0`): a home-tenant GUID, `organizations` (work and school), or `common` (work, school, and personal Microsoft accounts). `EntraIssuerValidator` still requires `iss` to match that token's own `tid`. A GUID authority can complete OIDC only for that tenant, even when the policy is Any or lists other ids. Accepting another tenant, or any tenant, needs authority `organizations` or `common` **and** an app registration whose supported account types are multi-tenant.

`dev entra setup` creates `AzureADMyOrg` (this tenant only). For other tenants, change supported account types to `AzureADMultipleOrgs` (work and school) or `AzureADandPersonalMicrosoftAccount` (also personal accounts) and set `TenantId` to `organizations` or `common`.

**Security.** Any-tenant means any account that can finish OpenID Connect against this app registration can sign up or link, still subject to the bootstrap and link toggles. There is no tenant blocklist. Personal Microsoft accounts use tenant id `9188040d-6c67-4c5b-b112-36a304b66dad` (`EntraTenantIds.Consumers`). They reach the app only when the authority is `common` or `consumers`. `organizations` never issues those tokens. Restricted mode accepts that id only when an administrator lists it.

Each accepted tenant is a tenant id plus an optional domain. Sign-in still matches the token `tid` only. The domain is how the admin found the id: `/Admin/Authentication` looks up a primary domain (`contoso.com`) through `GET https://login.microsoftonline.com/{domain}/.well-known/openid-configuration` and stores the GUID from the `issuer` (`https://login.microsoftonline.com/{tenant-id}/v2.0`). A tenant id pasted by hand has no domain. The list shows `contoso.com (guid)`, or the bare GUID when there is no domain.

First boot copies a GUID `TenantId` into the restricted list as an entry with no domain, so an existing home-tenant deployment keeps accepting that tenant. `organizations` and `common` are not tenant ids, so they seed an empty restricted list until an administrator chooses a list or Any. A row created before this column existed is filled the same way once (`EntraTenantAcceptanceInitialized`). Rows stored as a JSON array of GUID strings are migrated to entries with a null domain; acceptance does not change. `dev entra reseed` rewrites offer and bootstrap only; it leaves the list and the mode.

## First administrator

The first human passkey registration calls `IPrincipalRoleStore.TryClaimFirstAdministratorAsync` from `CompletePasskeyRegistration`. On an empty deployment that claim assigns Administrator and Member. Administrator includes `settings.write` and `admin.access`, so that person can open `/Admin/Authentication` and set tenant acceptance. A later passkey stays Member. The same claim runs for the first Entra bootstrap create; whichever human row lands first wins. `Authentication:BootstrapAdministratorPrincipalIds` remains optional break-glass and is not required for this path. No second bootstrap is added.
