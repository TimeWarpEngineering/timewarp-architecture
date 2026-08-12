# Disposition — task 182 (permission-centric authorization)

**Status: ACCEPTED** (2026-08-12) after round-1 (Claude) + round-2 (Grok).

## Review state

| Round | Reviewer | Artifact | Verdict |
|-------|----------|----------|---------|
| 1 | Claude (Fable 5) | `review/round-1/claude.md` | Accept with amendments (6 blocking, 7 non-blocking) |
| 2 | Grok | `review/round-2/grok.md` | **Confirm** Accept with amendments; no blocking contests |

## Decision

**Accept** the permission-centric architecture in `research/decision-brief.md` and `task.md`, with the amendments below folded into implementation children.

### Target model (accepted)

- Enforcement names **permissions** (capabilities), never product role Guids.
- Roles are **mutable bundles of permissions** (composition + admin UX).
- Single in-process **`IPermissionEvaluator`** as the decision seam; cookie stays PrincipalId-only; expand per-request.
- Resource-based checks when instance identity matters.
- External PDP (OpenFGA/Cedar/SpiceDB) optional behind the evaluator port — not required in AppHost.
- Greenfield bootstrap: first Create account → Administrator **role**; seed grants that role `admin.*` permissions. Recovery: `dev db reset`.

### Rejected

- RequireRole / RolePolicyGrants as long-term enforcement SSOT.
- COPIC parity / modules as the product ceiling.
- Mandatory external PDP.
- Sign-in backfill for pre-permission principals.

---

## §7 answers (closed)

| # | Question | Disposition |
|---|----------|-------------|
| 1 | Permission id format | **Dotted lowercase strings** `<area>.<concern>.<verb>` (`admin.roles.manage`). Not Guid, not dual. Stability = “do not rename issued ids.” |
| 2 | Policy vs permission | **1:1** — policy name **is** the permission id string. Retire `CanView*` for permission-backed policies. Keep `Authenticated` / `Anonymous` / scheme-composition exceptions until agent child. **Admin read/manage split required** in seed. |
| 3 | Claims vs evaluate | **Per-request via `IPermissionEvaluator`**. Handler always routes through evaluator. Claims projection only as **internal** optimization of default evaluator. `GetCurrentSession` returns expanded permissions from same evaluator for SPA. Scheme-aware from day one. |
| 4 | Placement | Features **substrate** (like RoleIds / IPrincipalRoleStore). **One** registry class replaces both `AuthorizationConstants.Policies` and `AuthorizationPolicyNames`. Folder home coordinates with task **132**. |
| 5 | ModuleRequirement / ModuleIds | **Delete in Phase 1 (182-C)**. Not Phase 4. Not “keep for demos.” |
| 6 | Phase 1 boundary | **Three sequential children A→B→C** (model / server swap / SPA swap + dead-code delete). Seed keeps B→C observably equivalent; no consumer release mid-window. |
| 7 | Agent scopes | **Scopes = permission bundles for agents** (parallel to roles for humans). Own child after A–C; keep scheme restrictions on admin. |

---

## Blocking amendments (all accepted)

1. **Split Phase 1 into three sequential children** — model (no enforcement change) → server enforcement swap → SPA swap + dead-code delete.
2. **Single permission registry + single registration helper** used by SPA and server; dual constant classes die.
3. **`IPermissionEvaluator` is the only decision seam** (ADR contract); claims not the handler contract.
4. **ModuleRequirement / ModuleIds / AuthorizationState.Modules deleted in 182-C** (Phase 1), not Phase 4.
5. **Lockout guards with Phase 2 UI:** last-admin on principal role strip + protected-core on Administrator (prefer system role whose core `admin.*` is not removable). Do not ship editable bundles without these; they serve as resource-check exemplars.
6. **Admin read/manage split** in seed (`admin.roles.read` / `admin.roles.manage`, principals likewise).

---

## Non-blocking amendments (accepted; schedule as noted)

| # | Item | When |
|---|------|------|
| 7 | Analyzer: EndpointAuthorize policy must be registry constant | Child or late Phase 1 / Phase 4 |
| 8 | Agent scope → permission bundles + scheme-aware evaluator cleanup | Child after A–C |
| 9 | Generator-emitted AuthSchemes from policy metadata (158) | Follow-up |
| 10 | Delete orphan CanViewAdminPage / CanViewUserClaims | Fold into 182-C |
| 11 | Naming convention + ADR draft with 182-A | 182-A / Phase 4 accept |
| 12 | api-server out of Phase 1 | Explicit on B/C |
| 13 | Entra branch note in ADR | ADR only |

---

## Child tasks (cut after this disposition)

Use `ganda kanban create "…" --parent 182`:

| Order | Child theme | Notes |
|-------|-------------|--------|
| A | Model: registry, role→permission store, seed, evaluator, tests | No user-visible enforcement change |
| B | Server enforcement swap | Admin contracts + program.cs; integration tests read≠manage |
| C | SPA swap + dead-code delete | Session permissions; kill RolePolicyGrants/constants/modules; SPA authz tests |
| D | Phase 2: Roles UI permission membership + lockout guards | Includes last-admin + protected-core |
| E | Phase 4: ADR accept + seam docs + remaining debt | ADR drafted with A |
| F | Agent scope → permission bundles | After A–C |
| G | (Optional) Analyzer for policy registry constants | Non-blocking |
| H | (Optional) External PDP packaging/docs | Phase 5 |

Phase 3 resource exemplars: **fold primary lockout exemplars into D**; additional resource examples only if needed later.

---

## Session

- 2026-08-12: PENDING until Grok round-2.
- 2026-08-12 (Grok): round-2 confirms Accept with amendments; disposition **ACCEPTED**; children to cut next.
