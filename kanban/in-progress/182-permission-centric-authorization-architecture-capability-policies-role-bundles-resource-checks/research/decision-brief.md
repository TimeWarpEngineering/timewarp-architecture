# Decision brief: permission-centric authorization for the template

**Status:** Proposed architecture for review (not implementation disposition).  
**Audience:** Implementers + Claude review.  
**Context date:** 2026-08-12.

---

## 1. Problem statement

The template must teach and ship a **best-in-class authorization architecture** for generated apps (Blazor WASM + FastEndpoints + passkey humans + agent keys). Current enforcement uses **role identity** (`RequireRole` / `RolePolicyGrants`). That is a shortcut: surfaces couple to role Guids; roles cannot be rebundled without code; server and SPA maps can drift; resource-level access has no home.

COPIC used **modules + ModuleRequirement** (better separation: policies name capabilities, roles bag modules). That is **not** the ceiling and **not** the product shape to clone (ERP modules, SPA-only state as PDP).

---

## 2. Research summary (industry 2024–2026)

### Models

| Model | Decision basis | Failure mode |
|-------|----------------|--------------|
| RBAC | Actor kind / job title | Role explosion; multi-tenant sharing |
| ABAC / PBAC | Attributes + policy rules | Attribute sprawl; weak sharing graphs |
| ReBAC (Zanzibar) | Relationship graph | Ops cost if forced before need |

Consensus: serious systems **stack** models; enforcement names **capabilities**, not roles; roles are **bundles**.

### Engines

| Engine | Strength | Template day-one? |
|--------|----------|-------------------|
| OpenFGA / SpiceDB | ReBAC at scale | No as **required**; yes as optional port |
| Cedar | Analyzable ABAC policy-as-code | Optional; not sole story |
| ASP.NET policies + handlers | Native, testable, no extra process | **Default PDP host** |

### ASP.NET Core

Microsoft guidance: **policy-based authorization**; resource-based checks via `IAuthorizationService.AuthorizeAsync(user, resource, policy)`. Role claims are a convenient claim type, not the architecture.

---

## 3. Template constraints

- Unknown consumer domains → domain-agnostic permission registry.
- Local Aspire + optional postgres → in-process default.
- Humans + agents → one permission vocabulary; agents also use scopes.
- Generated endpoints → policy on contracts (`[EndpointAuthorize]`).
- TWA0009 → grant/permission substrate placement careful.
- Greenfield → `dev db reset`; no legacy principal migration paths.

---

## 4. Rejected options

| Option | Why rejected as template **default** |
|--------|--------------------------------------|
| Keep RequireRole + RolePolicyGrants as SSOT | Surfaces name roles; rebundle requires code |
| COPIC parity (modules UI + ModuleRequirement only) | Coarse; ERP vocabulary; incomplete resource story |
| Mandatory OpenFGA/SpiceDB in AppHost | Ops/consistency burden for every consumer |
| Claims-only / no grant store | Admin UI and rebundle impossible |
| Sign-in backfill for old principals | Greenfield template; wipe DB |

---

## 5. Target architecture

### Layers

```text
1. ENFORCEMENT  Policy name = permission/capability (code registry)
2. DECISION     IPermissionEvaluator + handlers (+ optional resource)
3. GRANTS       principal → role(s) → permissions
                later: direct grants, ReBAC relations
```

### Vocabulary

| Term | Meaning |
|------|---------|
| Permission | Atomic capability (stable id) |
| Role | Named set of permissions (data) |
| Policy | ASP.NET name for enforcement (prefer 1:1 with permission) |
| Relation | ReBAC edge (later) |
| Resource | Instance for fine-grained AuthorizeAsync |

### Evaluation

1. Global capability: subject has permission P (direct or via role expansion).
2. Resource: subject has P on resource R (handler + resource arg).
3. Context: MFA, env, agent scope as conditions on the same decision path.
4. Agents: map scopes into permission ids where meaningful.

### Admin UI (makes sense under this model)

| Screen | Responsibility |
|--------|----------------|
| Permissions | Read-only catalog from code registry |
| Roles | Edit permission membership of a role |
| Principals | Assign roles to principals |
| (No policy CRUD) | Policies stay code |

### External PDP

`IPermissionEvaluator` implementation swappable. Document OpenFGA/Cedar as consumer option. Not required in template host by default.

### First-admin

First Create account claims **Administrator role**; seed gives that role all `admin.*` permissions. Endpoints check permissions, not role name.

---

## 6. Phased delivery (scope only)

| Phase | Deliverable |
|-------|-------------|
| 1 Model | Registry, seed, evaluator, replace RequireRole enforcement for current surfaces |
| 2 UI | Roles × permissions editing; optional catalog page |
| 3 Resource | Exemplar resource policies + tests |
| 4 Seam | ADR, docs, retire RolePolicyGrants/ModuleIds debt |
| 5 Optional | External PDP packaging/docs |

---

## 7. Review questions (must answer in disposition)

1. Permission id format: Guid vs dotted string vs dual?
2. Policy name 1:1 with permission, or keep `CanView*` aliases?
3. Expand to claims at session vs evaluate store every request?
4. Type placement (substrate/platform) under TWA0009?
5. Fate of ModuleRequirement + ERP ModuleIds?
6. Phase 1 boundary vs more children?
7. Agent scope ↔ permission mapping shape?

---

## 8. Hotspots (current code)

| Area | Path |
|------|------|
| SPA RolePolicyGrants | `web-spa/features/authorization/role-policy-grants.cs` |
| ModuleRequirement | `web-spa/.../custom-requirements/` |
| Server RequireRole | `web-server/program.cs` |
| RoleIds | `features/admin/roles/role-ids-contracts.cs` |
| ModuleIds (ERP) | `features/admin/modules/module-ids-contracts.cs` |
| Effective roles | `IEffectiveRolesResolver` |
| First admin | `TryClaimFirstAdministratorAsync` |
| DB wipe | `dev db reset --yes` |

---

## 9. Verdict (for reviewers to accept or amend)

**Accept:** permission-centric enforcement; roles as bundles; in-process PDP; resource-ready; external PDP optional behind port; greenfield bootstrap via Administrator role seed + Create account / db reset.

**Do not accept without change:** RequireRole as product architecture; mandatory Zanzibar in template; COPIC UI/module catalog as the target shape.
