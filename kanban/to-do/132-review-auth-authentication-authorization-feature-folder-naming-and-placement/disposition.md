# Disposition — task 132 (auth / authentication / authorization naming)

**Status: accepted** (implementation review clean, 2026-09-04). Naming review only. No product-code moves on this id.

The original brief described three near-empty `web/features/{auth,authentication,authorization}` peers. That snapshot is stale. **104-016 / 104-021 / 182** already collapsed the server-side collision. What remains is a glossary, a host-family map for **118**, and one accepted SPA rename.

See `inventory.md` for the file/namespace map this answers.

---

## Glossary (accepted)

| Word | Means | Does not mean |
|------|-------|----------------|
| **Identity** | Principal, credentials, ceremonies, session — *prove who*. | Permission checks, role CRUD, Entra-as-a-product-slice. |
| **Authorization** | Permissions, evaluator, role→permission / scope→permission grants — *what you may do*. | Login, passkeys, “auth”. |
| **Admin** | Human CRUD for roles and principal↔role assignment. | The authorization engine (that is Authorization substrate). |
| **Session / current-user** | Signed-in projection (`GetCurrentSession`). Not a top-level slice name. | A third peer of identity and authorization. |
| **Auth** | Forbidden as a folder or slice id. | — |

**Do not call things:** `auth/`, a new `authentication/` under `web/features/`, or a fourth `api/features/auth*` tree when marketplace slices land.

---

## Chosen taxonomy

| Concern | Folder (human home) | Namespace | Host family |
|---------|---------------------|-----------|-------------|
| Identity (prove who) | `web/features/identity/` | `Features.Identity` (+ `.Application` / `.Infrastructure` layers) | **web** (human plane). Agent *ceremonies* stay web (human approves). Agent *API consumption* already has an api-server sample; marketplace endpoints target **api**. |
| Identity scheme-name catalog | `web/features/identity/authentication-scheme-names-contracts.cs` | `Features` (substrate) | **web today.** Api does not reference this type yet (sample uses string / `AgentTokenDefaults` literals). Dual-host should reuse this catalog, not a third scheme-name copy. |
| Identity host wiring (web) | `web/platform/identity-host/` | Non-Features (`Abstractions` / `Services` / `Configuration`) | **web** — cookie/session + mock principal + agent-token defaults |
| Identity host wiring (api) | `api/platform/identity-host/` | Mixed: `Configuration` / `Abstractions` / `Services` / `Infrastructure`; handler is `Features.Identity` | **Already present** (not future). Bearer validation + duplicated `AgentTokenDefaults`. Token claim-type strings (`Scheme`, `ScopeClaimType`, principal-id claim) must stay aligned with web; policy-name constants already differ (`agent-scope:*` vs web `PermissionIds` / historical `identity.read`). Do not invent `api/features/auth*` or a third defaults class. |
| Authorization engine + permission catalog | `web/features/authorization/` | **`Features` substrate** (folder is the concern; types are shared). EF store: `Features.Authorization.Infrastructure` | **web now.** Dual-host the *evaluator + catalog* onto api when marketplace policies exist — **reuse these types**, do not invent `api/features/authorization` as a second engine. |
| Role catalog constants | `web/features/admin/roles/role-ids-contracts.cs` | `Features` (substrate) | **web today.** Api does not reference `RoleIds`. Dual-host should reuse this catalog, not duplicate it. |
| Admin catalog CRUD | `web/features/admin/{roles,principals}/` | `Features.Admin.Roles` / `Features.Admin.Principals` | **web** human plane |
| SPA identity UI (passkeys) | `web-spa/features/identity/` | `Features.Identity` | spa |
| SPA authorization (policies + Entra/mock grants cache) | `web-spa/features/authorization/` | `Features.Authorization` (constants: `TimeWarp.Architecture`) | spa |
| SPA Entra adapters + default login/logout | `web-spa/features/authentication/` + `web-spa/features/account/` | `Features.Authentication` / `Features.Account` | spa — **accepted fold into identity** (follow-on) |
| Api agent-bearer teaching sample | `api/features/agent-bearer-sample/` | `Features.AgentBearerSamples` | **api** — keep as sample, not an identity slice dual |

---

## Answers

### 1. Are these three different concerns?

**Not three product-slice peers.** The original `auth` ≈ `authentication` ≈ leftover era folders next to a living `identity/`. After 104/182:

- **Identity** and **Authorization** *are* two different concerns (prove who vs what you may do). Those names do not collide if **Auth** and **Authentication** are not peers.
- **Admin** is a third folder, but it is catalog management, not a synonym of either.
- Bare **Auth** is not a concern. It was one dormant Passwordless contract. Deleted.

SPA still presents three near-synonyms (`authentication` + `identity` + `account`) plus `authorization`. That *is* the remaining collision — see reject/defer/do-now.

### 2. Does `auth/` earn a top-level slice?

**No.** Already deleted (104-016). **Reject** resurrection. **Reject** folding anything into a new `auth/` name. Remaining Passwordless notes on 104-016/104-021 are closed.

### 3. Is `GetCurrentUser` authentication or authorization (or neither)?

**Neither of the original folder names, and not “who am I”.**

| Contract | Job | Placement |
|----------|-----|-----------|
| `GetCurrentSession` | Ambient cookie session: PrincipalId + RoleIds + Permissions from `IPermissionEvaluator` | `features/identity/` — **live** `[ApiEndpoint]` |
| `GetCurrentUser` | Client-only **grants projection** for SPA mock + Entra `AccountClaimsPrincipalFactoryWithRoles` | `features/identity/` — `[ClientOnlyContract]`, **no handler** |

SPA `AuthorizationState` fetches `GetCurrentUser` only on the Entra/mock path. Identity-session projects grants from `GetCurrentSession` and does not need that cache.

The type name still sounds like who-am-I. **Defer** renaming to `GetCurrentUserGrants` (mock factory + Entra factory + tests; Design region already documents the split). **Reject** hosting it as a public endpoint.

### 4. Is `RoleIds` a product slice at all?

**No.** It is Features substrate (compile-time Guids many slices share). Path `features/admin/roles/role-ids-contracts.cs` + namespace `Features` already match the placement skill. **Reject** moving it again. **Reject** `Features.Authorization` or `Features.Admin.Roles` for this file (TWA0009).

`PermissionIds` is the same pattern under `features/authorization/` — substrate catalog, not a slice type.

The original “path says authorization / namespace says Features” smell is **closed** for RoleIds. For the authorization *engine*, the same substrate choice is **intentional** (182 disposition §7 Q4): Identity and Admin must call `IPermissionEvaluator` without a cross-slice opt-out on every file. Folder name `authorization/` is the human home; namespace stays bare `Features`. Do not “fix” that by making it `Features.Authorization` product-isolated.

### 5. How do these relate to `features/identity/`?

Crisp boundary:

| Slice / cluster | Owns |
|-----------------|------|
| **Identity** | Who: principal store, passkeys, agent keys, session cookie, agent-token *issuance* |
| **Authorization** | What you may do: `PermissionIds`, evaluator, role/scope grant stores, policy registration |
| **Admin** | Humans editing the role catalog and principal membership |
| **identity-host (platform)** | HTTP/cookie/scheme wiring — not a product noun |

Identity handlers *call* the evaluator; they do not own permission ids. Admin CRUD *authorizes with* `PermissionIds`; it does not own the engine. That is the opposite of a fourth near-synonym.

### 6. 118 host-role mapping (web vs api)

Task 118: **web = human plane** (passkey/session), **api = agent plane** (bearer/x402). Do not invent `api/features/auth` / `authentication` / `authorization` as a parallel tree.

| Piece | 118 placement |
|-------|----------------|
| Passkey ceremonies, `GetCurrentSession`, `EndBrowserSession`, credential management | **web-only** |
| Agent key *registration* (human approval) | **web-only** (lands in the human plane) |
| Agent token *issuance* | **web today.** Marketplace agent traffic consumes tokens on **api**. Do not silently move issuance in 132. 118 may dual-host or keep issuance on web and validation on both. |
| `GetAgentIdentity` (`api/identity/agent/me`) | **web today.** Api already has `GetAgentBearerIdentity` sample (`api/agent/bearer/me`) — teaching, not a dual identity slice. Marketplace should extend **api** agent routes, not copy web identity. |
| `GetCurrentUser` | **web-SPA mock/Entra only** — never api |
| Permission catalog + `IPermissionEvaluator` | **web now; dual-host onto api** when marketplace endpoints need policies. Same `PermissionIds` strings. Web-only port `IAgentPermissionScopeSource` exists specifically to avoid dual-host type collision — api will need its own host adapter, not a second engine. Api does **not** reference `PermissionIds` today. |
| Admin roles/principals CRUD | **web-only** (human admin UX) |
| `RoleIds` / `PermissionIds` / `AuthenticationSchemeNames` | **Web Features substrate today.** Api does not reference these types (sample + `AgentTokenDefaults` literals only). Dual-host should **reuse** the catalogs rather than duplicate them. Neither family should grow a second catalog. |
| `api/platform/identity-host/` | **Already live** — agent-token scheme, handler parity, caller context, bearer-store module (validation only). Keep it as the api host cluster. Do not copy into `api/features/auth*`. Token claim-type strings must stay aligned; policy-name constants already differ. Catalog reuse / evaluator on api is **118**, not 132. |
| Template-demo scaffolding | Identity + x402 ship with the template (104-021: no extra flags). 118’s `real-domain` flag gates *marketplace nouns*, not a new auth stack. |

---

## Reject / defer / do now

| Item | Verdict | Why |
|------|---------|-----|
| Resurrect `features/auth/` or any `Auth` slice | **Reject** | Deleted; name collides with authn∪authz. |
| Re-host `GetSignInToken` / `GetCurrentUser` as public endpoints | **Reject** | Security note on 110; GetCurrentUser is mock/Entra-only. |
| Move `RoleIds` again | **Reject** | Path + substrate namespace already aligned. |
| Make authorization engine `Features.Authorization` (TWA0009-isolated product slice) | **Reject** | 182: substrate so Admin/Identity consume the evaluator. Folder name is enough. |
| Move `features/authorization/` to `platform/` | **Reject** | Product meaning (permission catalog + grants) survives if a deployable is deleted — Features tree, not host bootstrap. |
| Rename `GetCurrentUser` → `GetCurrentUserGrants` | **Defer** | Design region is honest; rename is mock/Entra/test churn with no behavior change. |
| Dual-host evaluator / agent-token validation onto api-server | **Defer to 118** | Validation host cluster already exists (`api/platform/identity-host/`). Remaining work is catalog reuse (`PermissionIds` / `AuthenticationSchemeNames`) + evaluator, not a new auth tree. Recorded so 118 does not start `api/features/auth` or a third `AgentTokenDefaults`. |
| Unify duplicated `AgentTokenDefaults` (web vs api platform copies) | **Defer to 118** | 104-030: separate deployables. Token claim-type strings (`Scheme`, `ScopeClaimType`, principal-id claim) must stay aligned. Policy-name constants already differ (web historical `identity.read` / `demo.invoke` vs api `agent-scope:*`). Not a 132 rename; do not treat the classes as identical. |
| Fold SPA `authentication/` + `account/` login UX into `identity/` | **Do now (follow-on child)** | Last remaining three-sibling collision. Mechanical; keep `/authentication/{action}` (MSAL convention) and `/Login` `/Logout` routes. |
| Fold SPA `authorization/` into identity | **Reject** | Real concern; matches server `authorization/` folder. |
| Bulk-rename on this task id mixed with behavior | **Reject** | Brief: decision + small follow-on. |

---

## Follow-on

Child **132-001** (`ganda kanban create --parent 132`, published to origin-home to-do): **Fold SPA authentication and account login UX into identity**.

- Rehome `web-spa/features/authentication/*` and `pages/Authentication.razor` / `RedirectToLogin.razor` under `web-spa/features/identity/` (or `pages/` with `Features.Identity`).
- Rehome `LoginPage` / `LogoutPage` under identity. Inspect `AccountState` (`WalletAddress` etc.): delete if dead, otherwise keep only session fields under identity — do not invent a fourth slice for leftover wallet demo state.
- Namespace `Features.Authentication` and `Features.Account` → `Features.Identity`. Update `_Imports.razor` / `global-usings.cs` / tests (`login-return-url-tests`).
- **Keep** route `/authentication/{action}` (Entra `RemoteAuthenticatorView` convention) and `/Login` `/Logout`.
- **Keep** `web-spa/features/authorization/` and `web-spa/services/identity-session-*` (services stay artifact bootstrap).
- No server contract moves. No GetCurrentUser rename.

118 does **not** get a 132 child. Marketplace scaffolding reads this disposition: identity + authorization only; no `auth` peer on api.
