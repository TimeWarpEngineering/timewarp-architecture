# Permission-centric authorization (capabilities, role bundles, evaluator seam)

* Status: proposed
* Architect: Steven T. Cramer
* Consulted: Claude review (round-1), Grok review (round-2); kanban 182 disposition accepted 2026-08-12
* Date: 2026-08-12

Technical Story: kanban 182 (permission-centric authorization); children 182-001…182-006

## Context and Problem Statement

The template must teach and ship a **best-in-class authorization architecture** for generated apps
(Blazor WASM + FastEndpoints + passkey humans + agent keys). Enforcement today couples product
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

Concrete shape (target; phased delivery via 182 children):

* **Permission registry** — dotted lowercase `const string` ids (`admin.roles.manage`). Policy name
  **is** the permission id (1:1). Stability = do not rename issued ids.
* **Roles as data** — product roles (`RoleIds`) map to permission sets via
  `IRolePermissionStore` (dual-mode in-memory + EF). Seed: Administrator → admin.* + self-service;
  Member → self-service; Developer → developer.* + self-service; Operator → self-service until
  marketplace policies.
* **`IPermissionEvaluator`** — sole decision seam. Default impl expands
  principal → effective roles → permissions. Scheme-aware: human session schemes expand; agent-token
  empty until scope→permission bundles (182-006).
* **Resource checks** — when instance identity matters, handlers use
  `IAuthorizationService.AuthorizeAsync(user, resource, policy)` with the same permission vocabulary.
* **External PDP** — optional replacement of the default evaluator implementation; not required in
  AppHost.
* **Retire** role-identity enforcement maps (`RolePolicyGrants`, dual `CanView*` constant classes)
  and dead COPIC leftovers (`ModuleRequirement` / ERP `ModuleIds`) as enforcement SSOT — delivery
  in 182-002 / 182-003.

### Positive Consequences

* Surfaces can rebundle roles without redeploying policy attributes
* Admin UI has a natural model (permissions catalog, role membership, principal role assignment)
* Single evaluator port for tests, mock principals, and future OpenFGA adapters
* Admin read/manage split teaches fine-grained capabilities without permission explosion

### Negative Consequences

* Phase-1 window where SPA still evaluates roles while server evaluates permissions (seed must keep
  them observably equivalent; children ordered A→B→C)
* More moving parts than RequireRole alone (registry + grant store + evaluator + handler)
* Protected-core / last-admin lockout guards required before shipping editable bundles (182-004)

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
* Bad, because multi-child migration and temporary dual evaluation window

## Links

* Disposition: `kanban/in-progress/182-permission-centric-authorization-…/disposition.md`
* Model child: kanban **182-001** (registry, store, seed, evaluator — no enforcement swap)
* Related: ADR-0009 (Postgres EF golden path — role_permissions table co-located in identity schema)
* Accept formal status: planned under **182-005**

<!-- markdownlint-disable-file MD013 -->
