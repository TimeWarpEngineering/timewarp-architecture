# Permission-centric authorization (capabilities, role bundles, evaluator seam)

* Status: accepted
* Architect: Steven T. Cramer
* Consulted: Claude review (round-1), Grok review (round-2); kanban 182 disposition accepted 2026-08-12
* Date: 2026-08-12

Technical Story: kanban 182 (permission-centric authorization); children 182-001…182-006

## Context and Problem Statement

The template must teach and ship a **best-in-class authorization architecture** for generated apps
(Blazor WASM + FastEndpoints + passkey humans + agent keys). Enforcement previously coupled product
surfaces to **role identity** (`RequireRole` / SPA `RolePolicyGrants` → role Guids). That is a
shortcut: surfaces name roles; roles cannot be rebundled without code changes; server and SPA maps
can drift; resource-level access has no home.

What should every generated app inherit as the **default authorization architecture**?

## Decision Drivers

* Enforcement points must never name product roles — only **capabilities** (permissions)
* Roles remain useful as **admin-editable bundles** of permissions (composition + UX)
* Single decision path for server and SPA (no dual SSOT maps that can drift)
* Greenfield bootstrap: first Create account → Administrator **role**; seed grants `admin.*`
* Optional external PDP (OpenFGA / Cedar / SpiceDB) without rewriting handlers
* Humans + agents share one permission vocabulary; agents also use scopes
* Cookie stays PrincipalId-only (no baked grants; rebundle takes effect next request)
* TWA0009: shared vocabulary lives in Features substrate, not cross-slice product references

## Considered Options

* **A — Keep RequireRole + RolePolicyGrants as long-term SSOT**
* **B — COPIC parity** (modules + ModuleRequirement as the product ceiling)
* **C — Mandatory external PDP** (OpenFGA/SpiceDB required in AppHost)
* **D — Permission-centric enforcement** with roles as mutable permission bundles; in-process
  `IPermissionEvaluator` as the decision seam; external PDP optional behind the port

## Decision Outcome

Chosen option: **D — Permission-centric authorization**, because it separates enforcement vocabulary
from role composition, matches ASP.NET policy-based guidance, and leaves a clean upgrade path to
external PDPs and resource checks without forcing ops cost on every template consumer.

### As shipped (182-001…004)

* **Permission registry** — dotted lowercase `const string` ids (`admin.roles.manage`). Policy name
  **is** the permission id (1:1). Stability = do not rename issued ids. See `PermissionIds`.
* **Roles as data** — product roles (`RoleIds`) map to permission sets via
  `IRolePermissionStore` (dual-mode in-memory + EF `identity.role_permissions`). Seed: Administrator →
  admin.* + self-service; Member → self-service; Developer → developer.* + self-service; Operator →
  self-service until marketplace policies.
* **`IPermissionEvaluator`** — sole decision seam. Default `PermissionEvaluator` expands
  principal → effective roles → permissions for human session schemes; for **agent-token** expands
  ambient scopes via `IAgentCallerContext` + `AgentScopePermissionSeed` only (never human roles;
  **182-006** shipped).
* **Server enforcement** — `PermissionRequirement` + `PermissionRequirementHandler` call the evaluator
  only (never role or permission claim bags on the cookie principal). Scheme lists stay on
  `[EndpointAuthorize(AuthenticationSchemes)]`.
* **SPA** — `GetCurrentSession` expands permissions via the evaluator under `identity-session`;
  `IdentitySessionAuthenticationStateProvider` projects them as `PermissionIds.ClaimType` claims for
  `RequireClaim` policies. Cookie remains PrincipalId-only (147-004 D8).
* **Resource / lockout** — last-admin and protected-core guards (182-004) sit above the store; same
  permission vocabulary for instance-sensitive decisions when handlers use
  `IAuthorizationService.AuthorizeAsync(user, resource, policy)`.
* **External PDP** — optional replacement of the default evaluator implementation only; **not**
  required in AppHost. See
  [How to swap the permission evaluator for an external PDP](../../../how-to-guides/how-to-swap-permission-evaluator-for-external-pdp.md).
* **Retired as enforcement SSOT** — `RolePolicyGrants`, dual `CanView*` constant classes,
  `ModuleRequirement` / ERP `ModuleIds` as product gates; claim-based `agent-scope:*` and
  `credential-management` assertion policies on web-server (replaced by `PermissionIds` + evaluator).

### Entra branch (when next touched)

When `Authentication:UseEntra` is true, the SPA may still have a separate grant path
(`GetCurrentUser` / `AccountClaimsPrincipalFactoryWithRoles` → claims). That path is **not** the
identity-session SSOT. **Whenever the Entra branch is next touched, migrate SPA permission claims to
the same source as the default path:** server-expanded permissions from `IPermissionEvaluator`
(today: `GetCurrentSession.Permissions` via `IdentitySessionAuthenticationStateProvider`). Do not grow
a second permission map for Entra. This ADR does **not** implement that migration.

### Positive Consequences

* Surfaces can rebundle roles without redeploying policy attributes
* Admin UI has a natural model (permissions catalog, role membership, principal role assignment)
* Single evaluator port for tests, mock principals, and future OpenFGA/Cedar adapters
* Admin read/manage split teaches fine-grained capabilities without permission explosion

### Negative Consequences

* More moving parts than RequireRole alone (registry + grant store + evaluator + handler)
* Protected-core / last-admin lockout guards required for editable bundles (shipped under 182-004)
* Historical multi-child migration (temporary dual SPA/server evaluation window closed by 182-003)

## Pros and Cons of the Options

### A — Keep RequireRole + RolePolicyGrants

* Good, because minimal code and familiar ASP.NET role claims
* Bad, because surfaces couple to role Guids; rebundle requires code; server/SPA maps drift
* Bad, because resource-level access has no home

### B — COPIC parity (modules only)

* Good, because policies name capabilities somewhat
* Bad, because ERP module vocabulary is wrong for this product; ModuleRequirement never gated Admin
* Bad, because coarse and incomplete for resource story

### C — Mandatory external PDP

* Good, because industry ReBAC at scale
* Bad, because ops/consistency burden for every greenfield consumer
* Bad, because overkill as **required** template default

### D — Permission-centric + optional external PDP (chosen)

* Good, because enforcement names capabilities; roles stay editable bundles
* Good, because in-process default is Aspire-friendly; external PDP is a port swap
* Good, because scheme-aware evaluator supports humans and agents on one vocabulary
* Bad, because multi-child migration and temporary dual evaluation window (closed by 182-003)

## Links

* How-to (consumer PDP swap):
  [how-to-swap-permission-evaluator-for-external-pdp.md](../../../how-to-guides/how-to-swap-permission-evaluator-for-external-pdp.md)
* Disposition: `kanban/in-progress/182-permission-centric-authorization-architecture-capability-policies-role-bundles-resource-checks/disposition.md`
* Code: `PermissionIds`, `IPermissionEvaluator`, `PermissionEvaluator`,
  `AgentScopePermissionSeed`, `PermissionRequirementHandler`, registration in
  `web-server/program.cs` (`AddScoped<IPermissionEvaluator, PermissionEvaluator>()`)
* Children: **182-001** model · **182-002** server · **182-003** SPA · **182-004** Roles UI ·
  **182-005** this ADR + seam docs · **182-006** agent scopes (shipped)
* Related: [ADR-0009](0009-postgres-ef-golden-persistence-path.md) (Postgres EF golden path —
  `role_permissions` co-located in identity schema)

<!-- markdownlint-disable-file MD013 -->
