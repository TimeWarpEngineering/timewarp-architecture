# Permission-centric authorization architecture (capability policies, role bundles, resource checks)

## Description

Replace the template’s **role-identity enforcement** (`RequireRole` / `RolePolicyGrants` as the long-term SSOT) with a **permission-centric** authorization architecture suitable for a greenfield product template—not COPIC parity, not “what is common,” not shortcuts.

**Problem today**

- Enforcement couples product surfaces to **role Guids** (`RoleIds.Administrator`, SPA `RolePolicyGrants` → `RequireRole`).
- Roles cannot be rebundled without code changes; new surfaces edit a static map.
- Server (`web-server/program.cs`) and SPA composition can drift.
- No first-class **resource-level** path (instance checks).
- COPIC-shaped leftovers exist (`ModuleRequirement`, `ModuleIds`, `AuthorizationState.Modules`) but **do not** gate Admin; ERP sample modules are the wrong product vocabulary.
- Admin UI has Roles + Principals but no clear **authorization composition** story (roles as permission bundles).

**Target (end state)**

```text
Enforcement: policy name = capability/permission (code SSOT registry)
     ↓
Decision:    IPermissionEvaluator / PermissionRequirementHandler
             (optional resource for instance decisions)
     ↓
Grants:      principal → role(s) → permissions
             (+ later: direct grants, ReBAC relations)
```

- **Permissions** = atomic capabilities (stable ids in a compile-time registry).
- **Roles** = mutable **bundles of permissions** (admin-editable data).
- **Policies** = ASP.NET names aligned 1:1 (or documented fixed conjunction) with permissions.
- **Resource-based** checks when the decision needs an instance.
- **Agents**: scopes map into the same permission vocabulary where possible.
- **External PDP** (OpenFGA / Cedar / SpiceDB): optional behind a port—not a day-one hard dependency.
- Greenfield bootstrap: first human passkey Create account still assigns **Administrator role** (which seeds all `admin.*` permissions)—no RequireRole hardwire on endpoints.

**Out of scope for this epic’s core**

- Making OpenFGA/SpiceDB/Cedar **required** in every generated app.
- Migrating historical principals on existing DBs (use `dev db reset`).
- COPIC product UI clone (Security Roles + Modules ERP catalog).

**In scope**

- Architecture ADR + permission registry + evaluator + replace admin/self-service/developer gates that currently use RequireRole maps.
- Role→permission seed data; Principals still assign roles; Roles UI grows to edit permission membership (phase).
- Seam for external evaluator; docs for consumers.
- Child tasks for phased implementation after review disposition.

## Requirements

### Architectural invariants (non-negotiable)

1. **Enforcement points never name product roles.** `[Page]`, `[EndpointAuthorize]`, and agent policies name **permissions** (capabilities), not `RoleIds.*`.
2. **Single decision path.** SPA and server both evaluate via the same permission semantics (expanded grants), not dual ad-hoc maps.
3. **Roles are composition only.** Assigning Administrator means “has the Administrator permission set,” not “policy X requires Administrator string.”
4. **Resource-level is first-class when needed.** Use `IAuthorizationService.AuthorizeAsync(user, resource, policy)` / resource requirements—not bigger roles.
5. **Server is authoritative.** SPA AuthorizeView is UX; API handlers must deny.
6. **Template default is in-process PDP.** External engines are optional swap-ins.
7. **Greenfield-only recovery.** Empty grant store + first Create account / `dev db reset`—no sign-in backfill paths.

### Functional requirements (phased—see Checklist + children after review)

**Phase 1 — Model**

- Permission registry (compile-time SSOT; Guid or string ids—disposition picks).
- Seed: product roles → default permission sets (Administrator includes all `admin.*`, Member self-service, Developer demos).
- `IPermissionEvaluator` + `PermissionRequirement` / handler registered SPA + server.
- Replace Admin + self-service + developer **SPA** `RolePolicyGrants` entries and **server** `RequireRole(Administrator)` admin policies with permission policies.
- First-admin continues to assign Administrator **role** (permissions ride the seed).
- Tests: expand logic; admin allow/deny; Member cannot hit admin APIs.

**Phase 2 — Admin UX**

- Roles UI: edit which permissions a role includes (matrix or multi-select from registry).
- Optional read-only Permissions catalog page (generated from registry).
- Principals remain “assign roles” (direct grants deferred unless review says otherwise).

**Phase 3 — Resource checks**

- At least one resource policy exemplars (e.g. last-admin protection; own-credential revoke already session-bound—document pattern).
- Handler tests with resource argument.

**Phase 4 — Seam + docs**

- Document evaluator port; how a consumer plugs OpenFGA/Cedar without rewriting endpoints.
- ADR under `documentation/developer/conceptual/architectural-decision-records/`.
- Retire/stop growing `RolePolicyGrants` as long-term SSOT; replace or hollow out.
- Align or replace ERP `ModuleIds` with permission registry vocabulary (disposition: delete sample ERP modules vs keep behind demo flag).

**Phase 5 — Optional (separate children, not blocking)**

- Template flag or package docs for OpenFGA/Cedar hosting.
- ReBAC relation store when a product domain needs sharing graphs.

### Review deliverables (before implementation waves)

Under this folder:

| Artifact | Purpose |
|----------|---------|
| `research/decision-brief.md` | Deep-research synthesis (models, stack ranking, target architecture) |
| `review/` | Claude (and other) review rounds—disposition before Phase 1 code |
| `disposition.md` | Post-review: accepted / amend / reject per section; id format; open questions closed |

Reviewers must address at least:

1. Permission id format: stable Guid (like RoleIds) vs dotted string vs both.
2. Policy name = permission id 1:1, or keep `CanView*` aliases mapped once?
3. Expand permissions into claims at session issue vs evaluate from store every check.
4. Server handler location (platform vs features substrate) and TWA0009.
5. Whether ModuleRequirement is deleted, adapted, or left for demos only.
6. Scope of Phase 1 vs split children.
7. Agent scope ↔ permission mapping approach.

### Constraints

- No temporal estimates in task text (AGENTS.md).
- Kanban children via `ganda kanban create --parent 182` only after disposition.
- Slice isolation TWA0009; feature placement skill for new files.
- Jaribu + Shouldly for new tests; co-located or suite per existing rules.
- Do not reintroduce Fixie/xUnit.

### Done criteria (epic)

- Disposition accepted and committed.
- Phase 1–4 children created (or explicitly deferred in disposition with reason).
- When implementation completes: Admin/product surfaces enforce **permissions**; roles only compose; ADR published; How to validate on each child Results.

## Checklist

### Spec / review (this folder)

- [x] Folder task created; research brief written
- [ ] Claude review round (artifacts under `review/`)
- [ ] Disposition written (`disposition.md`)
- [ ] Child tasks created for accepted phases (`--parent 182`)
- [ ] ADR drafted/accepted (Phase 4 or with Phase 1 per disposition)

### Implementation (children—track on children, mirror high level here)

- [ ] Phase 1: registry + evaluator + replace RequireRole/RolePolicyGrants enforcement
- [ ] Phase 2: Roles UI permission membership + optional catalog
- [ ] Phase 3: resource-based exemplar(s)
- [ ] Phase 4: seam docs + cleanup ModuleIds/RolePolicyGrants debt
- [ ] Phase 5: optional external PDP (only if disposition keeps it)

## Notes

### Related tasks

| Task | Relation |
|------|----------|
| **180** | First human passkey claims Administrator **role** — remains valid; endpoints stop requiring role by name |
| **147-*** | Admin roles/principals UI — becomes composition UI under this model |
| **132** | Auth/authentication/authorization **folder naming** — coordinate vocabulary with this epic (permissions vs modules) |
| **118** | Marketplace / host planes — permissions must not assume web-only forever |
| **161** | Credential management auth schemes research — orthogonal schemes; scopes map to permissions |

### Current hotspots (implementation map)

| Area | Today |
|------|--------|
| SPA grants | `web-spa/features/authorization/role-policy-grants.cs` |
| SPA modules (unused for Admin) | `custom-requirements/module-requirement*.cs`, `authorization-state` |
| Server admin policies | `web-server/program.cs` `RequireRole(Administrator)` |
| Role ids | `features/admin/roles/role-ids-contracts.cs` |
| Module ids (ERP sample) | `features/admin/modules/module-ids-contracts.cs` |
| Effective roles | `IEffectiveRolesResolver` / claims transform |
| First admin | `TryClaimFirstAdministratorAsync` + registration handler |
| DB wipe | `dev db reset --yes` |

### Explicit non-goals

- COPIC Security Roles + Modules UI parity.
- Shipping OpenFGA in AppHost by default.
- RequireRole sprinkled as “good enough.”
- Sign-in claim backfill for pre-permission principals.

## Session

- Created: Grok (2026-08-12) after deep research on template authz; user accepted permission-centric target and requested folder task + write-up for Claude review.
- Research: `research/decision-brief.md` in this folder.
- Next: Claude review → disposition → child tasks.
