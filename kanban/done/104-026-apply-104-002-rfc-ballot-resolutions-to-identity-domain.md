# Apply 104-002 RFC ballot resolutions to Identity domain

> **ARCHIVED — process residue (2026-07-17).** Violates agent-collaboration
> **Same task through fold-in**. Fold-in lives on host **104-002** (reopened
> in-progress). Do not revive this task; do not create similar “apply RFC”
> siblings.

## Parent

104

## Description

~~Fold in the resolved decisions from 104-002 RFC into Identity.~~ Superseded:
implement fold-in on **104-002** checklist / Results.

## Source RFC

`kanban/done/104-002-implement-principal-credential-and-trusttier-domain-model/rfc/rfc.md`

## Resolutions to implement (stop-the-line before 104-003)

| # | Topic | Resolution |
|---|-------|------------|
| 1 | Trust model | **C refined:** orthogonal `IsQuarantined` (or risk flag); constrained `Promote` / `Quarantine` / `ClearQuarantine` (no free `SetTrustTier`); birth floor — Provisional **or** Keyed only after first credential; named predicates (`IsFundedAndActive`) not ordinal `>=` |
| 2 | Enum zeros | **B:** reserve `0 = None/Unknown` for `PrincipalKind`, `TrustTier` progression values, `CredentialType`; reject None at Create/promote |
| 3 | CredentialId | **B:** add `CredentialId` record struct; store APIs use it |
| 7 | Update handle branch | **B:** delete dead handle-migration in `UpdateCredentialAsync`; document immutable type/handle |

## Document only / defer (not code-blockers for 003)

| # | Resolution |
|---|------------|
| 4 | **A:** keep shared in-memory refs; Design + Results document LWW and that handlers must `Update*` where hosts require it; no snapshot-on-get for Wave 1 |
| 5 | **Defer B:** `TimeProvider` when 104-006 needs clock control |
| 6 | **A:** last-write-wins documented; no concurrency token yet |
| 8 | **A:** keep `byte[]` copy-on-get |

## Checklist

- [x] Decision 1: risk flag + constrained transitions + birth floor + named predicates — landed on **104-002**
- [x] Decision 2: renumber enums + reject None — landed on **104-002**
- [x] Decision 3: CredentialId + store API update — landed on **104-002**
- [x] Decision 7: delete dead UpdateCredential handle branch + Design note — landed on **104-002**
- [x] Design regions updated to match resolutions — landed on **104-002**
- [x] Unit tests updated/expanded — landed on **104-002**
- [x] `dev build` 0/0; identity tests green — landed on **104-002**
- [x] RFC banner: folded in; 104-002 Results updated
- [x] Board close: process residue; `done` so parent **104** can close (do not revive)

## Notes

- Adversarial ballot challenged full dual-status over-model; resolution keeps B’s security intent with C’s Wave-1 size.
- Do not start 104-003 until this task is done (or explicitly unblocked by maintainer).

## Session

- Created: 2026-07-17 (rfc-ballot on 104-002)
- 2026-07-17: archived as process residue; fold-in on **104-002**
- 2026-08-26: marked **done** so parent **104** can close (ganda 187: archived counts as open)

## Results

Process residue, not a product kitchen. RFC fold-in landed on host **104-002** (same-task-through-fold-in). Do not revive this id; do not create similar “apply RFC” siblings.

This close moves the stub from `archived/` to `done/` so parent **104** can close (parent-done treats archived as open). No Identity code changes here.

### How to validate

**Smoke**

```bash
ganda kanban path 104-026
# Expect: …/kanban/done/104-026-apply-104-002-rfc-ballot-resolutions-to-identity-domain.md

ganda kanban path 104-002
# Expect: …/kanban/done/104-002-implement-principal-credential-and-trusttier-domain-model/task.md
```

**Expect**

- 104-026 is under `kanban/done/`.
- 104-002 Results record the RFC fold-in (D1–D3, D7 in code; D4/D6/D8 Design; D5 deferred).
- No product diff on this id.
