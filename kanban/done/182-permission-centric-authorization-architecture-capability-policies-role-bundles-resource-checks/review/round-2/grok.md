# Round-2 review — permission-centric authorization (task 182)

**Reviewer:** Grok (xAI), 2026-08-12.  
**Inputs:** `task.md`, `research/decision-brief.md`, `review/round-1/claude.md`, `disposition.md` (PENDING charge), code spot-checks on hotspots.

---

## 1. Verdict on round-1

**Confirm: Accept with amendments.**

Claude’s verdict and architecture assessment are correct. The permission-centric target is the right template architecture; RequireRole-as-enforcement is not “good enough”; COPIC modules are not a ceiling and (in this repo) are dead code, not a baseline to preserve.

I re-checked the strongest empirical claims:

| Claim | Spot-check | Status |
|-------|------------|--------|
| `ModuleRequirementHandler` never in DI | No `Add*AuthorizationHandler` registration; only type definition | **Confirmed** |
| `RoleStore` is in-memory stub, no role→permission storage | `role-store-application.cs` Design: “Stub until 147-004”; only Name/Description | **Confirmed** |
| Orphan policies `CanViewAdminPage`, `CanViewUserClaims` | Only constants + RolePolicyGrants entries; no `[Page]` / endpoint | **Confirmed** |
| Admin policies multi-copy drift | SPA grants + `program.cs` RequireRole + contract policy strings | **Confirmed** |

No contest on the overall accept. Amendments below are **confirm** or **confirm with refinement** (no blocking rejects).

---

## 2. Decision-brief §7 — confirm / contest

### Q1 — Permission id format

**Confirm: dotted lowercase strings** (`<area>.<concern>.<verb>`). Not Guid, not dual.

Additional agreement with Claude’s external-PDP argument: OpenFGA/Cedar/SpiceDB all want string ids; Guid would force a translation table at the seam we want clean. Stability is a **don’t rename issued ids** policy, same as RoleIds, not a Guid property.

**No contest.**

### Q2 — Policy name 1:1 with permission

**Confirm: 1:1 — policy name IS the permission id string.** Retire `CanView*` for permission-backed policies.

**Confirm** exceptions: `Authenticated`, `Anonymous`, and scheme/scope-composition policies until the agent-scope child lands.

**Confirm** admin **read/manage** split (`admin.roles.read` / `admin.roles.manage`, same for principals). A pure rename of surface policies would fossilize read=write and undercut the teaching value of the epic.

**Minor refinement (non-blocking):** seed table is good; keep Phase 1 permission count small. Resist inventing `nav.*` permissions — Claude’s `admin.access` for sidebar is right.

### Q3 — Claims at issue vs evaluate every request

**Confirm: evaluate per-request via `IPermissionEvaluator`; cookie stays PrincipalId-only** (preserve 147-004 D8).

**Confirm** the critical seam rule: the **authorization handler always calls the evaluator**. Claim projection may only be an **internal optimization of the default in-process evaluator**, never the contract of the handler. Otherwise OpenFGA/Cedar swaps break.

**Confirm** SPA path: `GetCurrentSession` returns expanded permissions from the **same evaluator**; SPA projects for AuthorizeView. That is one expansion source (server), not dual PDP.

**Confirm** scheme-awareness from day one (agents must not inherit human role expansion accidentally via claims transform).

**No contest.**

### Q4 — Placement / TWA0009

**Confirm: Features substrate pattern** (same family as `RoleIds`, `IPrincipalRoleStore`).

**Confirm** consolidating to **one** policy/permission registry class, killing dual `AuthorizationConstants.Policies` + `AuthorizationPolicyNames`.

**Confirm** folder home coordinates with task **132** rather than inventing a third auth taxonomy here.

**No contest.**

### Q5 — ModuleRequirement / ModuleIds

**Confirm: delete in Phase 1 (child 182-C), not Phase 4.** Dead three ways; ERP vocabulary wrong; contradicts single decision path for readers of the template.

**No contest.** Do not “keep for demos.”

### Q6 — Phase 1 split

**Confirm: three sequential children A → B → C.**

Reason is solid: there is **no** role→permission store today; Phase 1 as a single child hides standing up persistence. RequireRole must stay green through A; B swaps server; C swaps SPA + deletes dead code.

**Refinement (non-blocking but implementers must obey):** document the **B→C equivalence window** in each child: seed must mirror today’s role→surface grants so SPA (still roles) and server (permissions) do not disagree observably. Do not ship/release mid-window to consumers without both B and C.

### Q7 — Agent scopes

**Confirm: scopes as named permission bundles for agents** (structurally like roles for humans); separate child after Phase 1 A–C; keep scheme restrictions on admin; evaluator scheme-aware from day one.

**No contest.**

---

## 3. Blocking amendments (1–6) — confirm / contest

### 1. Split Phase 1 into three sequential children (model / server / SPA+delete)

**Confirm.** Blocking. Cut as parented children after disposition finalization.

Suggested titles (implementers may refine wording via `ganda kanban create --parent 182`):

- Model: registry + role-permission store + seed + evaluator + tests (no enforcement swap)
- Server: permission policies + handler + admin contracts + integration tests including read≠manage
- SPA: session permissions + registration helper + retire RolePolicyGrants/constants/modules + SPA authz tests

### 2. Single registry + single registration helper for both hosts

**Confirm.** Blocking. This is the structural fix for multi-copy drift. Both `AddAuthorizationCore` (SPA) and `AddAuthorizationBuilder` (server) consume the same helper. No second constants class survives.

### 3. `IPermissionEvaluator` sole decision seam

**Confirm.** Blocking. Write into ADR as the swap contract. Claims projection only inside default evaluator implementation.

### 4. ModuleRequirement / ModuleIds / AuthorizationState.Modules deleted in Phase 1 (182-C)

**Confirm.** Blocking. Scope of delete as Claude listed is correct.

### 5. Lockout guards ship with Phase 2 editing UI (last-admin + protected-core)

**Confirm.** Blocking for Phase 2, not Phase 1.

**Refinement:** Prefer Claude’s **simpler** protected-core option if both are viable: **Administrator is a system role whose core `admin.*` set is not removable**, plus **last-admin** on `SetPrincipalRoles` when stripping the last principal who still holds `admin.principals.manage` (or equivalent). That doubles as the resource-check teaching exemplar; a separate Phase 3 child can add a second exemplar later if needed, but **do not ship editable permission bundles without lockout**.

### 6. Admin read/manage split in seed vocabulary

**Confirm.** Blocking. Required for teaching value and least privilege.

---

## 4. Non-blocking amendments (7–13)

**All confirmed as non-blocking.** Prioritize when cutting children:

| # | Item | Scheduling note |
|---|------|-----------------|
| 7 | Analyzer on `[EndpointAuthorize(Policy=)]` registry constants | Own child or fold late Phase 1 / Phase 4 |
| 8 | Agent scope → permission bundles | Own child after 182-A–C |
| 9 | Generator-emitted AuthSchemes from policy metadata | Follow-up (158 debt); not Phase 1 gate |
| 10 | Delete orphan CanViewAdminPage / CanViewUserClaims | Fold into 182-C |
| 11 | Dotted naming + ADR | With 182-A ADR draft |
| 12 | api-server out of Phase 1 | Explicit on 182-B/C |
| 13 | Entra branch migration note | ADR note only until Entra path touched |

---

## 5. Non-goals

**All four confirmed** (mandatory OpenFGA, COPIC parity, sign-in backfill, RequireRole as product architecture).  
**Nuance confirmed:** `RoleIds` and role assignment **survive**; only enforcement-by-role-name dies.

---

## 6. Risks (agree / add)

| Risk | Position |
|------|----------|
| Phase 2 lockout / last-admin | Agree with Claude — highest product risk once bundles are editable |
| Dual registration drift | Agree — single helper is mandatory |
| SPA trust / recon catalog | Agree — session returns **caller** permissions only; catalog gated `admin.*` |
| B→C window | Add: treat as **release barrier** — do not advertise half-migrated template |
| Test gap SPA | Agree — 182-C must add SPA authz tests |

---

## 7. Charge complete

| Item | Result |
|------|--------|
| Round-1 verdict | **Confirmed** Accept with amendments |
| §7 answers | **All confirmed** (refinements non-blocking only) |
| Blocking amendments 1–6 | **All confirmed** (5 with simpler protected-core preference) |
| Contests that block children | **None** |

Disposition may move from PENDING → **ACCEPTED** and children may be cut with `--parent 182`.
