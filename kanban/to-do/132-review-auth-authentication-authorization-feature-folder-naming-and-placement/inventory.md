# Inventory — auth / authentication / authorization (task 132)

Snapshot of **this worktree** (`task/132-review-auth-authentication-authorization-feature-f`, tracking `origin/feature/overnight`). Paths are relative to `source/container-apps/web/` unless noted.

The original brief’s three peer folders under `features/` are **already gone or transformed**:

| Original brief folder | Current state |
|-----------------------|---------------|
| `features/auth/` | **Absent.** Passwordless `GetSignInToken` deleted on **104-016** (after 110 unhosted it as `[ClientOnlyContract]`). |
| `features/authentication/` | **Absent.** `GetCurrentUser` + `MockUserIds` folded into `features/identity/` on **104-021**. |
| `features/authorization/` | **Present and grown.** No longer a single `RoleIds` file. Permission engine from **182** lives here. `RoleIds` moved to `features/admin/roles/`. |

SPA still has `features/authentication/` and `features/authorization/` twins, plus `features/identity/` and `features/account/`.

---

## 1. `web/features/auth/` — gone

No directory. No `GetSignInToken` type in source. Do not resurrect.

---

## 2. `web/features/authentication/` — gone

No directory. Former occupants:

| Former file | Now |
|-------------|-----|
| `get-current-user/get-current-user-contracts.cs` | `features/identity/get-current-user/` — `Features.Identity` |
| `mock-user-ids-contracts.cs` | `features/identity/mock-user-ids-contracts.cs` — `Features.Identity` |

---

## 3. `web/features/authorization/` — live permission engine (182)

Human folder for **what you may do**. Almost every type is **Features substrate** (`namespace TimeWarp.Architecture.Features`) so Admin / Identity / SPA can consume without TWA0009. Only the EF store uses `Features.Authorization.Infrastructure`.

| File | Namespace | Live? |
|------|-----------|-------|
| `permission-ids-contracts.cs` | `Features` (substrate) | Yes — SSOT permission strings |
| `permission-ids-tests.cs` | `Features` | Co-located Jaribu |
| `permission-policy-registration-contracts.cs` | `Features` | Yes — `AddPermissionPolicies` / `AddPermissionClaimPolicies` |
| `permission-requirement-contracts.cs` | `Features` | Yes — server `IAuthorizationRequirement` |
| `permission-requirement-authorization-server.cs` | `Features` | Yes — FastEndpoints/ASP.NET handler via evaluator |
| `i-permission-evaluator-application.cs` | `Features` | Yes — decision seam |
| `permission-evaluator-application.cs` | `Features` | Yes |
| `permission-evaluator-tests.cs` | `Features` | Co-located Jaribu |
| `permission-claim-policies-tests.cs` | `Features` | Co-located Jaribu |
| `i-role-permission-store-application.cs` | `Features` | Yes |
| `in-memory-role-permission-store-application.cs` | `Features` | Yes — default when no postgres |
| `ef-role-permission-store-infrastructure.cs` | `Features.Authorization.Infrastructure` | Yes — postgres swap |
| `role-permission-grant-infrastructure.cs` | `Features` | Yes — EF row type |
| `role-permission-grant-entity-type-configuration-infrastructure.cs` | `Features.Authorization.Infrastructure` | Yes |
| `role-permission-seed-application.cs` | `Features` | Yes — human role → permission bundles |
| `agent-scope-permission-seed-application.cs` | `Features` | Yes — agent scope → permission bundles |
| `i-agent-permission-scope-source-application.cs` | `Features` | Yes — web-only port (avoids api dual-host CS0433) |
| `admin-lockout-guards-application.cs` | `Features` | Yes — last-admin / protected-core |

No `[ApiEndpoint]` contracts in this folder — it is engine + catalog, not HTTP operations.

---

## 4. `web/features/identity/` — live principal / credential / session story

Product slice `Features.Identity` (layer files use `.Application` / `.Infrastructure`). One substrate file sits here for humans: `authentication-scheme-names-contracts.cs` → `Features`.

### Slice-root (shared)

| File | Namespace | Notes |
|------|-----------|-------|
| `authentication-scheme-names-contracts.cs` | `Features` (substrate) | Scheme name strings for `[EndpointAuthorize]` |
| `mock-user-ids-contracts.cs` | `Features.Identity` | Mock SPA user Guids |
| `agent-token-authentication-scheme-server.cs` | `Features.Identity` | Live bearer scheme |
| `*-ceremony-application.cs`, `web-authn-*`, `identity-problems-*`, `*-options*` | `Features.Identity.Application` | Live |
| `ef-principal-store-infrastructure.cs`, credential/principal EF config, in-memory module | `Features.Identity.Infrastructure` | Live |

### Operations (contract + handler unless noted)

| Use-case | Markers | Host |
|----------|---------|------|
| `start-passkey-registration`, `complete-passkey-registration` | `[ApiEndpoint]` | web |
| `start-passkey-authentication`, `complete-passkey-authentication` | `[ApiEndpoint]` | web |
| `add-passkey`, `get-credentials`, `revoke-credential` | `[ApiEndpoint]` | web |
| `get-current-session` | `[ApiEndpoint]` + `[EndpointAllowAnonymous]` | web — **who am I** (cookie + expanded RoleIds + Permissions) |
| `end-browser-session` | `[ApiEndpoint]` | web |
| `start-agent-key-registration`, `complete-agent-key-registration`, `add-agent-key` | `[ApiEndpoint]` | web (human-approved) |
| `start-agent-token-issuance`, `complete-agent-token-issuance` | `[ApiEndpoint]` | web today |
| `get-agent-identity` | `[ApiEndpoint]` | web — `api/identity/agent/me` |
| **`get-current-user`** | **`[ClientOnlyContract]` only — no handler, no `[ApiEndpoint]`** | SPA mock / Entra grants projection |

Ingress tests assert `api/GetCurrentUser` is **not** a generated prefix.

---

## 5. `web/features/admin/` — catalog CRUD (not an “auth” synonym)

### `admin/roles/`

| File | Namespace | Live? |
|------|-----------|-------|
| `role-ids-contracts.cs` | **`Features` (substrate)** | Yes — Member / Operator / Administrator / Developer Guids |
| `role-details-contracts.cs` | `Features.Admin.Roles` | Shared bindable |
| `role-store-application.cs` | `Features.Admin.Roles.Application` | Yes |
| `create-role`, `update-role`, `delete-role`, `get-role`, `get-roles`, `set-role-permissions` | `Features.Admin.Roles` (+ `.Application` handlers) | All `[ApiEndpoint]` + handlers |

### `admin/principals/`

Principal↔role assignment. Mix of substrate (`IPrincipalRoleStore`, `IEffectiveRolesResolver`, `PrincipalRoleAssignment`) and slice (`Features.Admin.Principals`) for list/set operations. All list/set contracts are `[ApiEndpoint]`.

---

## 6. `web/platform/identity-host/` — host/platform cluster (not a product slice)

Non-`Features.<Id>` namespaces (`Abstractions`, `Services`, `Configuration`, `Web.Server`). Cookie session, mock principal handler, agent-token defaults, current-principal accessor. Exception: `agent-caller-permission-scope-source-server.cs` is `Features` substrate (adapter onto the authorization port).

---

## 7. SPA twins (`web-spa/features/` — grammar-exempt)

### `authentication/` — Entra / MSAL adapters + sign-in listener

| File | Namespace | Live? |
|------|-----------|-------|
| `AuthenticationStateListener.razor` + `.razor.cs` | `Features.Authentication` | Yes — sign-in/out cache fan-out (profile, authorization, credentials) |
| `account-claims-principal-factory-with-roles.cs` | `Features.Authentication` | Entra-only (`UseEntra=true`) |

Pages still in this namespace (under `web-spa/pages/`, not the feature folder):

| File | Route | Live? |
|------|-------|-------|
| `pages/Authentication.razor` | `/authentication/{action}` | Entra `RemoteAuthenticatorView` |
| `pages/RedirectToLogin.razor` | used as NotAuthorized | Default path → `/Login` (passkey), not MSAL |

### `authorization/` — SPA policies + mock/Entra grants cache

| File | Namespace | Live? |
|------|-----------|-------|
| `authorization-constants.cs` | `TimeWarp.Architecture` | Yes — `Anonymous` / `Authenticated` only |
| `policy-registration.cs` | `TimeWarp.Architecture` | Yes — claim policies from `PermissionIds` |
| `authorization-state/*.cs` | `Features.Authorization` | Yes — cache for **GetCurrentUser** (mock/Entra). Identity-session path does **not** use this cache; it projects `GetCurrentSession.Permissions`. |

### `identity/` — passkey credential UI

| File | Namespace | Live? |
|------|-----------|-------|
| `credentials-state/*` | `Features.Identity` | Yes |
| `pages/passkeys-page/PasskeysPage.razor(+.cs)` | `Features.Identity` | Yes |

### `account/` — default passkey login/logout UX

| File | Namespace | Live? |
|------|-----------|-------|
| `pages/login-page/LoginPage.razor(+.cs)` | `Features.Account` | Yes — default unauthorized landing |
| `pages/LogoutPage.razor(+.cs)` | `Features.Account` | Yes |
| `account-state/*` | `Features.Account` | Present; holds `Alias` / `WalletAddress` / `SessionToken` / `IsAuthenticated` — wallet fields look leftover vs passkey session |

---

## 8. SPA services (artifact folder, not a slice)

Default identity-session (non-Entra) lives under `web-spa/services/`, namespace `TimeWarp.Architecture.Services`:

- `identity-session-authentication-registration.cs`
- `identity-session-authentication-state-provider.cs`
- `no-op-access-token-provider.cs`
- `passkey-ceremony-client.cs`
- mocks: `mock-authentication-*.cs` (fail-closed via `MockAuthenticationRegistration`)

---

## 9. Api family (118-relevant)

`source/container-apps/api/features/` has **no** `auth/` / `authentication/` / `authorization/` / `identity/` trees.

| Path | Namespace | Role |
|------|-----------|------|
| `api/features/agent-bearer-sample/get-agent-bearer-identity/` | `Features.AgentBearerSamples` | Teaching sample on **api-server**: `api/agent/bearer/me`, `[EndpointAuthorize]` agent-token. Design region: **not** a dual of web `GET api/identity/agent/me`. Ceremonies stay on web. |

---

## 10. Identity ↔ authorization touchpoints

| From | To | How |
|------|----|-----|
| `GetCurrentSession` handler | `IPermissionEvaluator` | Expands Permissions onto the session response |
| SPA `IdentitySessionAuthenticationStateProvider` | `GetCurrentSession` | Projects Role + `permission` claims |
| SPA `AuthorizationState.FetchCurrentUser` | `GetCurrentUser` (client-only) | Mock/Entra only |
| `AccountClaimsPrincipalFactoryWithRoles` | `AuthorizationState` | `[CrossSliceReference]` — Entra grants |
| `AuthenticationStateListener` | Profile / Authorization / Credentials | `[CrossSliceReference]` |
| Admin role/principal handlers | `PermissionIds` + evaluator | `[EndpointAuthorize(Policy = PermissionIds.…)]` |
| Agent token | `AgentScopePermissionSeed` | Scope → permission ids; no `admin.*` |

---

## 11. Prior consolidations (do not re-litigate as if unmoved)

| Task | What landed |
|------|-------------|
| **110** | Unhosted `GetSignInToken` (account-takeover primitive) |
| **104-016** | Deleted Passwordless + emptied `features/auth/` |
| **104-021** | Folded `features/authentication/` into identity; `RoleIds` → `admin/roles/`; Entra non-default; **left SPA naming to 132** |
| **182** | Permission-centric engine under `features/authorization/`; deleted ModuleIds / RolePolicyGrants; `GetCurrentSession.Permissions` |
| **147-002** | Product RoleIds (Member/Operator/Administrator/Developer); ERP sample roles removed |
